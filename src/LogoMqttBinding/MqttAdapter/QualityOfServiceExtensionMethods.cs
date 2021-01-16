using System;
using LogoMqttBinding.Configuration;
using MQTTnet.Protocol;

namespace LogoMqttBinding.MqttAdapter
{
  public static class QualityOfServiceExtensionMethods
  {
    public static MqttQualityOfServiceLevel ToMqttNet(this MqttChannelConfigBase.QoS qos)
    {
      return qos switch
      {
        MqttChannelConfigBase.QoS.AtMostOnce => MqttQualityOfServiceLevel.AtMostOnce,
        MqttChannelConfigBase.QoS.AtLeastOnce => MqttQualityOfServiceLevel.AtLeastOnce,
        MqttChannelConfigBase.QoS.ExactlyOnce => MqttQualityOfServiceLevel.ExactlyOnce,
        _ => throw new ArgumentOutOfRangeException(nameof(qos), qos, null),
      };
    }
  }
}