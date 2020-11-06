using System;
using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace LogoMqttBinding
{
  //TODO: allow reading of values without change

  internal class Mapper
  {
    public Mapper(ILoggerFactory loggerFactory, Logo logo, Mqtt mqttClient)
    {
      this.logo = logo;
      mapping = new Mapping(loggerFactory, mqttClient);
    }

    public void WriteLogoVariable(Mqtt.Subscription subscription, int address, string type)
    {
      subscription.MessageReceived += (sender, args) =>
      {
        switch (type)
        {
          case "integer":
            mapping.ReceivedInteger(logo.IntegerAt(address), args.ApplicationMessage.Payload);
            break;

          case "byte":
            mapping.ReceivedByte(logo.ByteAt(address), args.ApplicationMessage.Payload);
            break;

          case "float":
            mapping.ReceivedFloat(logo.FloatAt(address), args.ApplicationMessage.Payload);
            break;
        }
      };
    }

    public NotificationContext PublishOnChange(string topic, int address, string type)
    {
      switch (type)
      {
        case "integer":
          return logo
            .IntegerAt(address)
            .SubscribeToChangeNotification(async logoVariable =>
              await mapping.PublishInteger(topic, logoVariable).ConfigureAwait(false));

        case "byte":
          return logo
            .ByteAt(address)
            .SubscribeToChangeNotification(async logoVariable =>
              await mapping.PublishByte(topic, logoVariable).ConfigureAwait(false));

        case "float":
          return logo
            .FloatAt(address)
            .SubscribeToChangeNotification(async logoVariable =>
              await mapping.PublishFloat(topic, logoVariable).ConfigureAwait(false));
      }

      throw new ArgumentOutOfRangeException(nameof(type), type, "should be integer, byte or float");
    }

    private readonly Logo logo;
    private readonly Mapping mapping;
  }
  
  
  
  
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