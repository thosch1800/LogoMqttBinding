using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{Topic} -> {Type}@{LogoAddress}")]
  public class MqttLogoChannelConfig : MqttChannelConfigBase
  {
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public int LogoAddress { get; set; } = -1;
    public string Payload { get; set; } = string.Empty;
    public int Duration { get; set; } = 250;
  }
}