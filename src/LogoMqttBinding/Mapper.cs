using System;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;

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

    public void MapLogoVariable(Mqtt.Subscription subscription, int address, string type)
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

    public NotificationContext NotifyOnChange(string topic, int address, string type)
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

    public void AddLogoGetValueHandler(Mqtt.Subscription subscription, string chType, string topic, int chLogoAddress)
    {
      subscription.MessageReceived += async (sender, args) =>
      {
        switch (chType)
        {
          case "integer":
            await mapping.PublishInteger(topic, logo.IntegerAt(chLogoAddress)).ConfigureAwait(false);
            break;

          case "byte":
            await mapping.PublishByte(topic, logo.ByteAt(chLogoAddress)).ConfigureAwait(false);
            break;

          case "float":
            await mapping.PublishFloat(topic, logo.FloatAt(chLogoAddress)).ConfigureAwait(false);
            break;
        }
      };
    }

    private readonly Logo logo;
    private readonly Mapping mapping;
  }
}