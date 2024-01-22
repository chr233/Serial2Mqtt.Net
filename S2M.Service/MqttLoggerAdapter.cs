using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Diagnostics;
using S2M.Infrastructure;

namespace S2M.Service;

/// <summary>
/// MqttNet日志适配器
/// </summary>
/// <param name="_logger"></param>
/// <param name="_option"></param>
public sealed class MqttLoggerAdapter(
    ILogger<MqttLoggerAdapter> _logger,
    IOptions<OptionsSetting> _option) : IMqttNetLogger
{
    /// <inheritdoc cref="IMqttNetLogger.IsEnabled"/>
    public bool IsEnabled => _option.Value.MqttServer.Logging;

    /// <inheritdoc cref="IMqttNetLogger.Publish"/>
    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception)
    {
        var level = logLevel switch {
            MqttNetLogLevel.Verbose => LogLevel.Debug,
            MqttNetLogLevel.Info => LogLevel.Information,
            MqttNetLogLevel.Warning => LogLevel.Warning,
            MqttNetLogLevel.Error => LogLevel.Error,
            _ => LogLevel.None
        };

#pragma warning disable CA2254 // 模板应为静态表达式
        if (parameters != null)
        {
            _logger.Log(level, message, parameters);
        }
        else
        {
            _logger.Log(level, message);
        }
#pragma warning restore CA2254 // 模板应为静态表达式

        if (exception != null)
        {
            _logger.LogError(exception, "Mqtt服务端遇到错误");
        }
    }
}