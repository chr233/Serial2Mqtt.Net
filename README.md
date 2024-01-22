# S2M.NET

串口转 Mqtt, 集成 Mqtt Broker 功能

例如串口 COM1 中接收的消息默认发往 COM1/RX 主题, 同时会订阅 COM1/TX 主题, 其中的会发送到串口 COM1

## 配置文件

`appsettings.json`

```json
{
  "SerialPorts": {
    "COM8": {
      "BaudRate": 19200,
      "Parity": 1,
      "DataBits": 8,
      "StopBits": 1
    }
  },
  "MqttServer": {
    "Port": 1883,
    "Logging": true
  }
}
```
