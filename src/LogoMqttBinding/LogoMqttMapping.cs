using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using MQTTnet;

namespace LogoMqttBinding
{
  internal static class LogoMqttMapping
  {
    //TODO: refactor to instance class (also featuring a logger)
    public static void AddMessageHandler(this Mqtt.Subscription subscription, Logo logo, string chType, int chLogoAddress)
    {
      subscription.MessageReceived += (sender, args) => LogoSetValue(logo, chType, chLogoAddress, args.ApplicationMessage);
    }

    private static void LogoSetValue(Logo logo, string type, int address, MqttApplicationMessage msg) => LogoSetValueFor[type].Invoke(logo, address, msg.Payload);

    private static readonly ImmutableDictionary<string, Action<Logo, int, byte[]>> LogoSetValueFor =
      new Dictionary<string, Action<Logo, int, byte[]>>
      {
        { "integer", LogoSetInteger },
        { "byte", LogoSetByte },
        { "float", LogoSetFloat },
      }.ToImmutableDictionary();

    private static void LogoSetInteger(Logo logo, int address, byte[] payload)
    {
      if (FromPayload(payload, out short value))
        logo.IntegerAt(address).Set(value);
    }

    private static void LogoSetByte(Logo logo, int address, byte[] payload)
    {
      if (FromPayload(payload, out byte value))
        logo.ByteAt(address).Set(value);
    }

    private static void LogoSetFloat(Logo logo, int address, byte[] payload)
    {
      if (FromPayload(payload, out float value))
        logo.FloatAt(address).Set(value);
    }



    public static void LogoNotifyOnChange(string type, Mqtt mqttClient, string topic, Logo logo, int address)
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



    private static bool FromPayload(byte[] payload, out float result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      //if (!succeeded) logger.Log();
      return succeeded;
    }

    private static bool FromPayload(byte[] payload, out byte result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = byte.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      return succeeded;
    }

    private static bool FromPayload(byte[] payload, out short result)
    {
      var s = Encoding.UTF8.GetString(payload);
      var succeeded = short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
      //if (!succeeded) logger.Log();
      return succeeded;
    }

    private static byte[] ToPayload(float value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    private static byte[] ToPayload(byte value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }

    private static byte[] ToPayload(short value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encoding.UTF8.GetBytes(s);
    }
  }
}