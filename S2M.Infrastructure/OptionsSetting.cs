using System.IO.Ports;
using System.Text.Json.Serialization;

namespace S2M.Infrastructure;

/// <summary>
/// 配置文件
/// </summary>
public sealed record OptionsSetting
{
    /// <inheritdoc cref="MqttClientOption"/>
    public MqttClientOption MqttClient { get; set; } = new();

    /// <inheritdoc cref="SerialPortOption"/>
    public Dictionary<string, SerialPortOption> SerialPorts { get; set; } = [];

    /// <inheritdoc cref="MqttServerOption"/>
    public MqttServerOption MqttServer { get; set; } = new();

    /// <summary>
    /// Mqtt客户端设置
    /// </summary>
    public sealed record MqttClientOption
    {
        /// <summary>
        /// 系统话题
        /// </summary>
        public string SystemTopic { get; set; } = "/System";
        /// <summary>
        /// 串口收话题
        /// </summary>
        public string RxTopic { get; set; } = "/Rx";
        /// <summary>
        /// 串口发话题
        /// </summary>
        public string TxTopic { get; set; } = "/Tx";
        /// <summary>
        /// 串口错误话题
        /// </summary>
        public string ErrorTopic { get; set; } = "/Error";
    }

    /// <summary>
    /// 串口设置
    /// </summary>
    public sealed record SerialPortOption
    {
        /// <summary>
        /// 物理路径
        /// </summary>
        [JsonIgnore]
        public string? PhysicalName { get; set; }
        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; } = 9600;
        /// <summary>
        /// 奇偶校验
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;
        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; } = 8;
        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;
    }

    /// <summary>
    /// Mqtt服务端设置
    /// </summary>
    public sealed record MqttServerOption
    {
        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 1883;
        /// <summary>
        /// 启用日志
        /// </summary>
        public bool Logging { get; set; } = true;
    }
}
