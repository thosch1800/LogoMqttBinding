using System;
using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{Topic} -> {Type}@{LogoAddress}")]
  public class MqttChannelConfig
  {
    public string Action { get; set; } = Actions.Publish.ToString();
    public string Topic { get; set; } = "";
    public int LogoAddress { get; set; } = -1;
    public string Type { get; set; } = Types.Byte.ToString();

    public Actions GetActionAsEnum() => Enum.Parse<Actions>(Action, true);
    public Types GetTypeAsEnum() => Enum.Parse<Types>(Type, true);

    public enum Actions
    {
      Publish,
      Subscribe,
      SubscribePulse,
    }

    public enum Types
    {
      Byte,
      Integer,
      Float,
    }
  }
}