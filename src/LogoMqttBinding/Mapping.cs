using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace LogoMqttBinding
{
  internal class Mapping
  {
    public Mapping(ILoggerFactory loggerFactory, Mqtt mqttClient)
    {
      this.mqttClient = mqttClient;
      mqttFormat = new MqttFormat(loggerFactory.CreateLogger<MqttFormat>());
    }



    public void ReceivedInteger(ILogoVariable<short> logoVariable, byte[] payload)
    {
      if (mqttFormat.ToValue(payload, out short value))
        logoVariable.Set(value);
    }

    public void ReceivedByte(ILogoVariable<byte> logoVariable, byte[] payload)
    {
      if (mqttFormat.ToValue(payload, out byte value))
        logoVariable.Set(value);
    }

    public void ReceivedFloat(ILogoVariable<float> logoVariable, byte[] payload)
    {
      if (mqttFormat.ToValue(payload, out float value))
        logoVariable.Set(value);
    }



    public async Task PublishInteger(string topic, ILogoVariable<short> logoVariable)
    {
      var value = logoVariable.Get();
      await Publish(topic, mqttFormat.ToPayload(value)).ConfigureAwait(false);
    }

    public async Task PublishByte(string topic, ILogoVariable<byte> logoVariable)
    {
      var value = logoVariable.Get();
      await Publish(topic, mqttFormat.ToPayload(value)).ConfigureAwait(false);
    }

    public async Task PublishFloat(string topic, ILogoVariable<float> logoVariable)
    {
      var value = logoVariable.Get();
      await Publish(topic, mqttFormat.ToPayload(value)).ConfigureAwait(false);
    }



    private async Task Publish(string topic, byte[] payload)
    {
      await mqttClient
        .PublishAsync(new MqttApplicationMessageBuilder()
          .WithTopic(topic)
          .WithPayload(payload)
          .Build())
        .ConfigureAwait(false);
    }

    private readonly Mqtt mqttClient;
    private readonly MqttFormat mqttFormat;
  }
}