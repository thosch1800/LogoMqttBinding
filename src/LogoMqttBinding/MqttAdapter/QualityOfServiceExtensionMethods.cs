using System;
using LogoMqttBinding.Configuration;
using MQTTnet.Protocol;

namespace LogoMqttBinding.MqttAdapter
{
  public static class QualityOfServiceExtensionMethods
  {
    public static MqttQualityOfServiceLevel ToMqttNet(this MqttChannelConfig.QoS qos)
    {
      return qos switch
      {
        MqttChannelConfig.QoS.AtMostOnce => MqttQualityOfServiceLevel.AtMostOnce,
        MqttChannelConfig.QoS.AtLeastOnce => MqttQualityOfServiceLevel.AtLeastOnce,
        MqttChannelConfig.QoS.ExactlyOnce => MqttQualityOfServiceLevel.ExactlyOnce,
        _ => throw new ArgumentOutOfRangeException(nameof(qos), qos, null),
      };
    }
  }
}