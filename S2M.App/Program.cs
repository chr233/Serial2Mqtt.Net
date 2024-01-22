using NLog.Extensions.Logging;
using S2M.Infrastructure;
using S2M.Service;
using S2M.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// 配置类支持
services.AddOptions();
services.Configure<OptionsSetting>(builder.Configuration);

// NLog
services.AddLogging(loggingBuilder => {
    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(LogLevel.Debug);
    loggingBuilder.AddNLog();
});

// 添加服务
services.AddService();
// Web API
//services.AddWebAPI(builder.WebHost);

services.AddHostedService<MqttServerHostedService>();
services.AddHostedService<SerialIOHostedService>();

var app = builder.Build();

// Web API
//app.UseWebAPI();

app.Run();
