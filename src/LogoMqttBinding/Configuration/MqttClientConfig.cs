using System;
using System.Diagnostics;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{" + nameof(ClientId) + "}")]
  public class MqttClientConfig
  {
    public string ClientId { get; set; } = "";

    public MqttChannelConfig[] Channels { get; set; } = Array.Empty<MqttChannelConfig>();

    public MqttChannelConfig? LastWill { get; set; }

    public bool CleanSession { get; set; } = true;
  }
}