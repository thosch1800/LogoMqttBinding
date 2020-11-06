using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace LogoMqttBinding
{
  //TODO: refactor to instance class (also featuring a logger)
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

    private readonly Converter converter;
    private readonly ImmutableDictionary<string, Action<Logo, int, byte[]>> logoSetValueFor;
  }



  internal class Converter
  {
    public Converter(ILogger logger) => this.logger = logger;



    public bool Parse(byte[] payload, out byte result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = byte.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      if (!succeeded) logger.LogWarning($"Cannot parse '{s}'");
      return succeeded;
    }

    public bool Parse(byte[] payload, out short result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      if (!succeeded) logger.LogWarning($"Cannot parse '{s}'");
      return succeeded;
    }

    public bool Parse(byte[] payload, out float result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      if (!succeeded) logger.LogWarning($"Cannot parse '{s}'");
      return succeeded;
    }



    public byte[] Create(byte value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    public byte[] Create(short value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    public byte[] Create(float value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }



    private readonly ILogger logger;
  }
}