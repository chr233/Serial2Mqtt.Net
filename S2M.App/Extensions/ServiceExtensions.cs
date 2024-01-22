using S2M.Service;

namespace S2M.WebAPI.Extensions;

/// <summary>
/// 动态注册服务扩展
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// 注册引用程序域中所有有AppService标记的类的服务
    /// </summary>
    /// <param name="services"></param>
    public static void AddService(this IServiceCollection services)
    {
        services.AddSingleton<MqttServerHostedService>();

        services.AddTransient<SerialService>();

        services.AddTransient<MqttLoggerAdapter>();
    }
}
