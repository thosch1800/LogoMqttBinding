using System;

namespace LogoMqttBinding.Configuration
{
  public class Config
  {
    public LogoConfig[] Logos { get; set; } = Array.Empty<LogoConfig>();
    public string MqttBrokerUri { get; set; } = "127.0.0.1";
    public int MqttBrokerPort { get; set; } = 1883;
    public string? MqttBrokerUsername { get; set; } = null;
    public string? MqttBrokerPassword { get; set; } = null;
  }
}