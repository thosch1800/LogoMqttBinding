using System;
using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{" + nameof(IpAddress) + "}")]
  public class LogoConfig
  {
    public string IpAddress { get; set; } = "127.0.0.1";
    public MemoryRangeConfig[] MemoryRanges { get; set; } = Array.Empty<MemoryRangeConfig>();
    public MqttDevice[] Mqtt { get; set; } = Array.Empty<MqttDevice>();
  }
}