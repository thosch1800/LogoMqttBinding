using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("[{LocalVariableMemoryStart}-{LocalVariableMemoryEnd}]@{LocalVariableMemoryPollingCycleMilliseconds}ms")]
  public class MemoryRangeConfig
  {
    public int LocalVariableMemoryPollingCycleMilliseconds { get; set; } = 100;
    public int LocalVariableMemoryStart { get; set; }
    public int LocalVariableMemoryEnd { get; set; } = 850;
    public int LocalVariableMemorySize => LocalVariableMemoryEnd - LocalVariableMemoryStart;
  }
}