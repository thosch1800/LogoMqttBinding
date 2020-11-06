using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Byte = LogoMqttBinding.LogoAdapter.Byte;

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
      if (converter.ToValue(payload, out short value))
        logo.IntegerAt(address).Set(value);
    }

    private void SetByte(int address, byte[] payload)
    {
      if (converter.ToValue(payload, out byte value))
        logo.ByteAt(address).Set(value);
    }

    private void SetFloat(int address, byte[] payload)
    {
      if (converter.ToValue(payload, out float value))
        logo.FloatAt(address).Set(value);
    }


    private async Task PublishInteger(string topic, ILogoVariable<short> logoVariable)
    {
      var value = logoVariable.Get();
      await PublishPayload(topic, converter.ToPayload(value)).ConfigureAwait(false);
    }

    private async Task PublishByte(string topic, ILogoVariable<byte> logoVariable)
    {
      var value = logoVariable.Get();
      await PublishPayload(topic, converter.ToPayload(value)).ConfigureAwait(false);
    }

    private async Task PublishFloat(string topic, ILogoVariable<float> logoVariable)
    {
      var value = logoVariable.Get();
      await PublishPayload(topic, converter.ToPayload(value)).ConfigureAwait(false);
    }



    public NotificationContext LogoNotifyOnChange(string type, string topic, int address)
    {
      switch (type)
      {
        case "integer":
          return logo
            .IntegerAt(address)
            .SubscribeToChangeNotification(async logoVariable => await PublishInteger(topic, logoVariable).ConfigureAwait(false));

        case "byte":
          return logo
            .ByteAt(address)
            .SubscribeToChangeNotification(async logoVariable => await PublishByte(topic, logoVariable).ConfigureAwait(false));

        case "float":
          return logo
            .FloatAt(address)
            .SubscribeToChangeNotification(async logoVariable => await PublishFloat(topic, logoVariable).ConfigureAwait(false));
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
            await PublishInteger(topic, logo.IntegerAt(chLogoAddress)).ConfigureAwait(false);
            break;

          case "byte":
            await PublishByte(topic, logo.ByteAt(chLogoAddress)).ConfigureAwait(false);
            break;

          case "float":
            await PublishFloat(topic, logo.FloatAt(chLogoAddress)).ConfigureAwait(false);
            break;
        }
      };
    }

    private async Task PublishPayload(string topic, byte[] payload)
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