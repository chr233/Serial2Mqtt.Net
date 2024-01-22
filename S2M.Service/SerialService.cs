using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Server;
using S2M.Infrastructure;
using System.IO.Ports;
using System.Text;

namespace S2M.Service;

/// <summary>
/// 串口服务
/// </summary>
/// <param name="_logger"></param>
/// <param name="_options"></param>
public class SerialService(
    ILogger<SerialService> _logger,
    IOptions<OptionsSetting> _options)
{
    private string RxTopic { get; set; } = null!;
    private string TxTopic { get; set; } = null!;
    private string ErrorTopic { get; set; } = null!;
    private IManagedMqttClient MqClient { get; set; } = null!;
    private SerialPort Sp { get; set; } = null!;

    /// <summary>
    /// 初始化串口
    /// </summary>
    /// <param name="serialOption"></param>
    public void Init(OptionsSetting.SerialPortOption serialOption)
    {
        var baseName = serialOption.PhysicalName ?? "";
        if (!baseName.StartsWith('/'))
        {
            baseName = "/" + baseName;
        }

        var opt = _options.Value.MqttClient;

        TxTopic = baseName + opt.TxTopic;
        RxTopic = baseName + opt.RxTopic;
        ErrorTopic = baseName + opt.ErrorTopic;
        Sp = new SerialPort(serialOption.PhysicalName, serialOption.BaudRate, serialOption.Parity, serialOption.DataBits, serialOption.StopBits);
        Sp.DataReceived += SerialPortDataReceived;

        MqClient = new MqttFactory().CreateManagedMqttClient();
        MqClient.ApplicationMessageReceivedAsync += MqttMessageReceived;
    }

    /// <summary>
    /// 打开串口服务
    /// </summary>
    /// <returns></returns>
    public async Task Open()
    {
        if (!MqClient.IsConnected)
        {
            var port = _options.Value.MqttServer.Port;
            var clientId = "Port-" + Sp.PortName;
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", port)
                .WithClientId(clientId)
                .WithCleanStart(true)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .Build();

            var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(mqttClientOptions)
                .Build();

            await MqClient.StartAsync(managedMqttClientOptions);

            await MqClient.SubscribeAsync(TxTopic);

            _logger.LogInformation("开启串口服务 {id} 成功", clientId);
        }

        if (!Sp.IsOpen)
        {
            try
            {
                Sp.Open();
                _logger.LogInformation("开启串口 {id} 成功", Sp.PortName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开启串口 {id} 失败", Sp.PortName);
                await MqClient.EnqueueAsync(ErrorTopic, ex.Message, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
            }
        }
    }

    /// <summary>
    /// 关闭串口服务
    /// </summary>
    /// <returns></returns>
    public async Task Close()
    {
        if (Sp.IsOpen)
        {
            Sp.Close();
        }

        if (MqClient.IsConnected)
        {
            await MqClient.StopAsync(true);
        }

        _logger.LogInformation("串口服务 {id} 关闭", Sp.PortName);
    }

    /// <summary>
    /// 串口收到消息
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var length = Sp.BytesToRead;
        var buffer = new byte[length];

        try
        {
            Sp.Read(buffer, 0, length);
        }
        catch (Exception ex)
        {
            MqClient.EnqueueAsync(ErrorTopic, ex.Message, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce).RunSynchronously();
        }

        var payload = Convert.ToBase64String(buffer);

        MqClient.EnqueueAsync(RxTopic, payload, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
    }

    /// <summary>
    /// Mqtt收到消息
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private async Task MqttMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        var msg = args.ApplicationMessage;

        byte[]? payload = null;
        try
        {
            var base64Text = Encoding.UTF8.GetString(msg.PayloadSegment);
            payload = Convert.FromBase64String(base64Text);
        }
        catch (Exception ex)
        {
            var errorMsg = string.Format("串口 {0} 数据解码失败, 必须为 Base64 字符串", Sp.PortName);
            _logger.LogError(ex, errorMsg);
            await MqClient.EnqueueAsync(ErrorTopic, errorMsg, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
        }

        var success = false;

        if (Sp.IsOpen && payload != null)
        {
            try
            {
                Sp.Write(payload, 0, payload.Length);
                success = true;
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format("串口 {0} 发送数据失败, 尝试重启串口", Sp.PortName);
                _logger.LogError(ex, errorMsg);
                await MqClient.EnqueueAsync(ErrorTopic, errorMsg, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
            }
        }

        if (!success)
        {
            try
            {
                if (Sp.IsOpen)
                {
                    Sp.Close();
                }
                Sp.Open();
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format("重启串口 {0} 失败", Sp.PortName);
                _logger.LogError(ex, errorMsg);
                await MqClient.EnqueueAsync(ErrorTopic, errorMsg, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
            }
        }
    }
}
