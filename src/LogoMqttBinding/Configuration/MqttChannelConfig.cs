using System;
using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{Topic} -> {Type}@{LogoAddress}")]
  public class MqttChannelConfig
  {
    public string Topic { get; set; } = string.Empty;
    public int LogoAddress { get; set; } = -1;
    public string Action { get; set; } = Actions.Publish.ToString();
    public string Type { get; set; } = Types.Byte.ToString();
    public string QualityOfService { get; set; } = QoS.AtMostOnce.ToString();
    public bool Retain { get; set; } = false;
    public string Payload { get; set; } = string.Empty;

    public Actions GetActionAsEnum() => Enum.Parse<Actions>(Action, true);
    public Types GetTypeAsEnum() => Enum.Parse<Types>(Type, true);
    public QoS GetQualityOfServiceAsEnum() => Enum.Parse<QoS>(QualityOfService, true);

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

    public enum QoS
    {
      AtMostOnce = 0,
      AtLeastOnce = 1,
      ExactlyOnce = 2,
    }
  }
}