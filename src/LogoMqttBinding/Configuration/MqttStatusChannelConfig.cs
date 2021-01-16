﻿using System.Diagnostics;

namespace LogoMqttBinding.Configuration
{
  [DebuggerDisplay("{Topic} -> Status<{Type}>")]
  public class MqttStatusChannelConfig : MqttChannelConfigBase
  {
    public MqttStatusChannelConfig()
    {
      Retain = true;
      Action = Actions.Publish.ToString();
      Type = Types.String.ToString();
      QualityOfService = QoS.ExactlyOnce.ToString();
    }
  }
}