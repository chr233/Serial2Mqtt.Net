using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Server;
using S2M.Infrastructure;

namespace S2M.Service;
/// <summary>
/// Mqtt服务端
/// </summary>
/// <param name="_options"></param>
/// <param name="_consoleLogger"></param>
public sealed class MqttServerHostedService(
    IOptions<OptionsSetting> _options,
    MqttLoggerAdapter _consoleLogger) : IHostedService
{
    private MqttServer? MqServer { get; set; }

    /// <inheritdoc cref="IHostedService.StartAsync"/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opt = _options.Value.MqttServer;

        var mqttFactory = new MqttFactory(_consoleLogger);

        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint().WithDefaultEndpointPort(opt.Port)
            .WithMaxPendingMessagesPerClient(999)
            .Build();

        MqServer = mqttFactory.CreateMqttServer(mqttServerOptions);
        return MqServer.StartAsync();
    }

    /// <inheritdoc cref="IHostedService.StopAsync"/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return MqServer?.StopAsync() ?? Task.CompletedTask;
    }
}
