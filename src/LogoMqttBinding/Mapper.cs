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
      logger = loggerFactory.CreateLogger<Mapper>();
      converter = new Converter(loggerFactory.CreateLogger<Converter>());
      logoSetValueFor = new Dictionary<string, Action<Logo, int, byte[]>>
      {
        { "integer", LogoSetInteger },
        { "byte", LogoSetByte },
        { "float", LogoSetFloat },
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

    private void LogoSetInteger(Logo logo, int address, byte[] payload)
    {
      if (FromPayload(payload, out short value))
        logo.IntegerAt(address).Set(value);
    }

    private void LogoSetByte(Logo logo, int address, byte[] payload)
    {
      if (FromPayload(payload, out byte value))
        logo.ByteAt(address).Set(value);
    }

    private void LogoSetFloat(Logo logo, int address, byte[] payload)
    {
      if (FromPayload(payload, out float value))
        logo.FloatAt(address).Set(value);
    }


    public void AddLogoGetValueHandler(Mqtt.Subscription subscription, Logo logo, Mqtt mqttClient, string chType, string topic, int chLogoAddress)
    {
      subscription.MessageReceived += async (sender, args) =>
      {
        switch (chType)
        {
          case "integer":
            var intValue = logo.IntegerAt(chLogoAddress).Get();
            await MqttPublish(ToPayload(intValue)).ConfigureAwait(false);
            break;

          case "byte":
            var byteValue = logo.ByteAt(chLogoAddress).Get();
            await MqttPublish(ToPayload(byteValue)).ConfigureAwait(false);
            break;

          case "float":
            var floatValue = logo.FloatAt(chLogoAddress).Get();
            await MqttPublish(ToPayload(floatValue)).ConfigureAwait(false);
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
          var intVariableReference = logo.IntegerAt(address);
          intVariableReference.SubscribeToChangeNotification(async () =>
          {
            var value = intVariableReference.Get();
            var payload = ToPayload(value);
            await MqttPublish(payload).ConfigureAwait(false);
          });
          break;

        case "byte":
          var byteVariableReference = logo.ByteAt(address);
          byteVariableReference.SubscribeToChangeNotification(async () =>
          {
            var value = byteVariableReference.Get();
            var payload = ToPayload(value);
            await MqttPublish(payload).ConfigureAwait(false);
          });
          break;

        case "float":
          var floatVariableReference = logo.FloatAt(address);
          floatVariableReference.SubscribeToChangeNotification(async () =>
          {
            var value = floatVariableReference.Get();
            var payload = ToPayload(value);
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



    private bool FromPayload(byte[] payload, out float result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      //if (!succeeded) logger.Log();
      return succeeded;
    }

    private bool FromPayload(byte[] payload, out byte result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = byte.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      return succeeded;
    }

    private bool FromPayload(byte[] payload, out short result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      //if (!succeeded) logger.Log();
      return succeeded;
    }

    private byte[] ToPayload(float value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    private byte[] ToPayload(byte value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    private byte[] ToPayload(short value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }


    private readonly Converter converter;
    private readonly ILogger<Mapper> logger;
    private readonly ImmutableDictionary<string, Action<Logo, int, byte[]>> logoSetValueFor;
  }



  class Converter
  {
    public Converter(ILogger logger) => this.logger = logger;

    private byte[] Create(float value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    private bool Parse(byte[] payload, out float result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      if (!succeeded)
        logger.LogMessage(
          "Parse failed",
          args => args
            .Add(nameof(payload), payload)
            .Add(nameof(s), s),
          LogLevel.Warning);
      return succeeded;
    }

    private bool Parse(byte[] payload, out byte result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = byte.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      //if (!succeeded) logger.Log();
      return succeeded;
    }

    private byte[] Create(byte value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    private bool Parse(byte[] payload, out short result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      //if (!succeeded) logger.Log();
      return succeeded;
    }

    private byte[] Create(short value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    private readonly ILogger logger;
  }
}