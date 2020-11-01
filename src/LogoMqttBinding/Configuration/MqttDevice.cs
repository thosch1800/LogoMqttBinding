using System;
using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{" + nameof(ClientId) + "}")]
  public class MqttDevice
  {
    public string ClientId { get; set; } = "";
    public MqttChannel[] Subscribed { get; set; } = Array.Empty<MqttChannel>();
    public MqttChannel[] Published { get; set; } = Array.Empty<MqttChannel>();
  }
}