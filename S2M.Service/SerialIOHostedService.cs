using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using S2M.Infrastructure;
using System.Text;

namespace S2M.Service;
/// <summary>
/// Mqtt串口服务
/// </summary>
/// <param name="_logger"></param>
/// <param name="_options"></param>
/// <param name="_serviceProvider"></param>
public sealed class SerialIOHostedService(
    ILogger<SerialIOHostedService> _logger,
    IOptionsMonitor<OptionsSetting> _options,
    IServiceProvider _serviceProvider) : IHostedService
{
    private List<SerialService> Serials { get; init; } = [];

    private async void UpdateConfiguration(OptionsSetting appSettings, string? name)
    {
        _logger.LogDebug("刷新配置文件……");
        await CleanOldServices();
        await InitNewServices();
        _logger.LogDebug("刷新配置文件完成");
    }

    private async Task CleanOldServices()
    {
        await Task.WhenAll(Serials.Select(x => x.Close()).ToArray());
        Serials.Clear();
    }

    private Task InitNewServices()
    {
        foreach (var (name, opt) in _options.CurrentValue.SerialPorts)
        {
            opt.PhysicalName = name;
            var service = _serviceProvider.GetRequiredService<SerialService>();
            service.Init(opt);
            Serials.Add(service);
        }

        var tasks = Serials.Select(x => x.Open()).ToList();
        tasks.Add(UpdateRetainMessage());

        return Task.WhenAll(tasks);
    }

    private async Task UpdateRetainMessage()
    {
        var option = _options.CurrentValue;
        var clientOpt = option.MqttClient;
        var port = _options.CurrentValue.MqttServer.Port;
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", port)
            .WithClientId("Port Message")
            .WithCleanStart(true)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .Build();

        var mqttClient = new MqttFactory().CreateMqttClient();
        await mqttClient.ConnectAsync(options);

        var sb = new StringBuilder();
        sb.AppendLine(string.Format("Topic Option: {0}", clientOpt));

        foreach (var (name, opt) in option.SerialPorts)
        {
            sb.AppendLine(string.Format("Serial {0} Option: {1}", name, opt));
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(clientOpt.SystemTopic)
            .WithPayload(sb.ToString())
            .WithRetainFlag(true)
            .Build();

        mqttClient.PublishAsync(message).Wait();
        await mqttClient.DisconnectAsync();
    }

    /// <inheritdoc cref="IHostedService.StartAsync"/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _options.OnChange(UpdateConfiguration);
        return InitNewServices();
    }

    /// <inheritdoc cref="IHostedService.StopAsync"/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return CleanOldServices();
    }
}
