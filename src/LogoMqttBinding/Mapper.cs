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
    public Mapper(ILoggerFactory loggerFactory)
    {
      converter = new Converter(loggerFactory.CreateLogger<Converter>());
      logoSetValueFor = new Dictionary<string, Action<Logo, int, byte[]>>
      {
        { "integer", SetInteger },
        { "byte", SetByte },
        { "float", SetFloat },
      }.ToImmutableDictionary();
    }

    public void AddLogoSetValueHandler(Mqtt.Subscription subscription, Logo logo, string chType, int chLogoAddress)
    {
      subscription.MessageReceived += (sender, args) =>
      {
        if (logoSetValueFor.TryGetValue(chType, out var handler))
          handler.Invoke(logo, chLogoAddress, args.ApplicationMessage.Payload);
      };
    }

    private void SetInteger(Logo logo, int address, byte[] payload)
    {
      if (converter.Parse(payload, out short value))
        logo.IntegerAt(address).Set(value);
    }

    private void SetByte(Logo logo, int address, byte[] payload)
    {
      if (converter.Parse(payload, out byte value))
        logo.ByteAt(address).Set(value);
    }

    private void SetFloat(Logo logo, int address, byte[] payload)
    {
      if (converter.Parse(payload, out float value))
        logo.FloatAt(address).Set(value);
    }


    public void LogoNotifyOnChange(Logo logo, Mqtt mqttClient, string type, string topic, int address)
    {
      switch (type)
      {
        case "integer":
          logo.IntegerAt(address).SubscribeToChangeNotification(async logoVariable =>
          {
            var value = logoVariable.Get();
            var payload = converter.Create(value);
            await MqttPublish(payload).ConfigureAwait(false);
          });
          break;

        case "byte":
          logo.ByteAt(address).SubscribeToChangeNotification(async logoVariable =>
          {
            var value = logoVariable.Get();
            var payload = converter.Create(value);
            await MqttPublish(payload).ConfigureAwait(false);
          });
          break;

        case "float":
          logo.FloatAt(address).SubscribeToChangeNotification(async logoVariable =>
          {
            var value = logoVariable.Get();
            var payload = converter.Create(value);
            await MqttPublish(payload).ConfigureAwait(false);
          });
          break;

          async Task MqttPublish(byte[] payload)
          {
            await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
              .WithTopic(topic)
              .WithPayload(payload)
              .Build());
          }
      }
    }

    public void AddLogoGetValueHandler(Mqtt.Subscription subscription, Logo logo, Mqtt mqttClient, string chType, string topic, int chLogoAddress)
    {
      subscription.MessageReceived += async (sender, args) =>
      {
        switch (chType)
        {
          case "integer":
          {
            var value = logo.IntegerAt(chLogoAddress).Get();
            var payload = converter.Create(value);
            await MqttPublish(payload).ConfigureAwait(false);
          }
            break;

          case "byte":
          {
            var value = logo.ByteAt(chLogoAddress).Get();
            var payload = converter.Create(value);
            await MqttPublish(payload).ConfigureAwait(false);
          }
            break;

          case "float":
          {
            var value = logo.FloatAt(chLogoAddress).Get();
            var payload = converter.Create(value);
            await MqttPublish(payload).ConfigureAwait(false);
          }
            break;

            async Task MqttPublish(byte[] payload)
            {
              await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build());
            }
        }
      };
    }

    private readonly Converter converter;
    private readonly ImmutableDictionary<string, Action<Logo, int, byte[]>> logoSetValueFor;
  }
}