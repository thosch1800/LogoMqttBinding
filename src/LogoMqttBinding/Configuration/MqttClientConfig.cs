using System;
using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{" + nameof(ClientId) + "}")]
  public class MqttClientConfig
  {
    public string ClientId { get; set; } = "";
    public MqttChannelConfig[] Channels { get; set; } = Array.Empty<MqttChannelConfig>();
  }
}