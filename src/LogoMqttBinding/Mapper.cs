using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
      this.mqttClient = mqttClient;
      converter = new Converter(loggerFactory.CreateLogger<Converter>());

      logoSetValueFor = new Dictionary<string, Action<int, byte[]>>
      {
        { "integer", SetInteger },
        { "byte", SetByte },
        { "float", SetFloat },
      }.ToImmutableDictionary();
    }

    public void AddLogoSetValueHandler(Mqtt.Subscription subscription, string chType, int chLogoAddress)
    {
      subscription.MessageReceived += (sender, args) =>
      {
        if (logoSetValueFor.TryGetValue(chType, out var handler))
          handler.Invoke(chLogoAddress, args.ApplicationMessage.Payload);
      };
    }

    private void SetInteger(int address, byte[] payload)
    {
      if (converter.Parse(payload, out short value))
        logo.IntegerAt(address).Set(value);
    }

    private void SetByte(int address, byte[] payload)
    {
      if (converter.Parse(payload, out byte value))
        logo.ByteAt(address).Set(value);
    }

    private void SetFloat(int address, byte[] payload)
    {
      if (converter.Parse(payload, out float value))
        logo.FloatAt(address).Set(value);
    }


    public NotificationContext LogoNotifyOnChange(string type, string topic, int address)
    {
      switch (type)
      {
        case "integer":
          return logo
            .IntegerAt(address)
            .SubscribeToChangeNotification(
              async logoVariable =>
              {
                var value = logoVariable.Get();
                await MqttPublish(topic, converter.Create(value)).ConfigureAwait(false);
              });

        case "byte":
          return logo
            .ByteAt(address)
            .SubscribeToChangeNotification(
              async logoVariable =>
              {
                var value = logoVariable.Get();
                await MqttPublish(topic, converter.Create(value)).ConfigureAwait(false);
              });

        case "float":
          return logo
            .FloatAt(address)
            .SubscribeToChangeNotification(
              async logoVariable =>
              {
                var value = logoVariable.Get();
                await MqttPublish(topic, converter.Create(value)).ConfigureAwait(false);
              });
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
          {
            var value = logo.IntegerAt(chLogoAddress).Get();
            await MqttPublish(topic, converter.Create(value)).ConfigureAwait(false);
          }
            break;

          case "byte":
          {
            var value = logo.ByteAt(chLogoAddress).Get();
            await MqttPublish(topic, converter.Create(value)).ConfigureAwait(false);
          }
            break;

          case "float":
          {
            var value = logo.FloatAt(chLogoAddress).Get();
            await MqttPublish(topic, converter.Create(value)).ConfigureAwait(false);
          }
            break;
        }
      };
    }

    async Task MqttPublish(string topic, byte[] payload)
    {
      await mqttClient
        .PublishAsync(new MqttApplicationMessageBuilder()
          .WithTopic(topic)
          .WithPayload(payload)
          .Build())
        .ConfigureAwait(false);
    }

    private readonly Logo logo;
    private readonly Mqtt mqttClient;
    private readonly Converter converter;
    private readonly ImmutableDictionary<string, Action<int, byte[]>> logoSetValueFor;
  }
}