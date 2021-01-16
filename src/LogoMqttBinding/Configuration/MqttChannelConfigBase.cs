using System;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace LogoMqttBinding.Configuration
{
  public abstract class MqttChannelConfigBase
  {
    public string Topic { get; set; } = string.Empty;
    public string Action { get; set; } = Actions.Publish.ToString();
    public string Type { get; set; } = Types.Byte.ToString();
    public string QualityOfService { get; set; } = QoS.AtMostOnce.ToString();

    public bool Retain { get; set; } = false;

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
      String
    }

    public enum QoS
    {
      AtMostOnce = 0,
      AtLeastOnce = 1,
      ExactlyOnce = 2,
    }
  }
}