using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{Topic} -> {Type}@{LogoAddress}")]
  public class MqttChannel
  {
    public string Topic { get; set; } = "";
    public int LogoAddress { get; set; } = -1;
    public string Type { get; set; } = "";
  }
}