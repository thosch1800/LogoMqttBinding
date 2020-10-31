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

  [DebuggerDisplay("{Topic} -> {Type}@{LogoAddress}")]
  public class MqttChannel
  {
    public string Topic { get; set; } = "";
    public int LogoAddress { get; set; } = -1;
    public string Type { get; set; } = "";
  }
}