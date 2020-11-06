using System;
using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{" + nameof(ClientId) + "}")]
  public class MqttClient
  {
    public string ClientId { get; set; } = "";
    public MqttChannel[] Publish { get; set; } = Array.Empty<MqttChannel>();
    public MqttChannel[] Subscribe { get; set; } = Array.Empty<MqttChannel>();
  }
}