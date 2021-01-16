using System;
using System.Diagnostics;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{" + nameof(ClientId) + "}")]
  public class MqttClientConfig
  {
    public string ClientId { get; set; } = "logo-mqtt";

    public MqttLogoChannelConfig[] Channels { get; set; } = Array.Empty<MqttLogoChannelConfig>();

    public MqttStatusChannelConfig? Status { get; set; }

    public bool CleanSession { get; set; } = true;
  }
}