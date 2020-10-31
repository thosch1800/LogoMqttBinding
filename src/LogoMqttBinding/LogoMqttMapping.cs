using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using MQTTnet;

namespace LogoMqttBinding
{
  internal static class LogoMqttMapping
  {
    public static void AddMessageHandler(this Mqtt.Subscription subscription, Logo logo, string chType, int chLogoAddress)
    {
      subscription.MessageReceived += (sender, args) => LogoSetValue(logo, chType, chLogoAddress, args.ApplicationMessage);
    }

    private static void LogoSetValue(Logo logo, string type, int address, MqttApplicationMessage msg) => LogoSetValueFor[type].Invoke(logo, address, msg.Payload);

    private static void LogoSetInteger(Logo logo, int address, byte[] payload)
    {
      var value = BitConverter.ToInt16(payload);
      logo.IntegerAt(address).Set(value);
    }

    private static void LogoSetByte(Logo logo, int address, byte[] payload)
    {
      var value = payload.First();
      logo.ByteAt(address).Set(value);
    }

    private static void LogoSetFloat(Logo logo, int address, byte[] payload)
    {
      var value = BitConverter.ToSingle(payload);
      logo.FloatAt(address).Set(value);
    }

    private static readonly ImmutableDictionary<string, Action<Logo, int, byte[]>> LogoSetValueFor =
      new Dictionary<string, Action<Logo, int, byte[]>>
      {
        { "integer", LogoSetInteger },
        { "byte", LogoSetByte },
        { "float", LogoSetFloat },
      }.ToImmutableDictionary();



    public static void LogoNotifyOnChange(string type, Mqtt mqttClient, string topic, Logo logo, int address)
    {
      switch (type)
      {
        case "integer":
          var intVariableReference = logo.IntegerAt(address);
          intVariableReference.SubscribeToChangeNotification(async () =>
          {
            var value = intVariableReference.Get();
            var payload = BitConverter.GetBytes(value);
            await MqttPublish(payload).ConfigureAwait(false);
          });
          break;

        case "byte":
          var byteVariableReference = logo.ByteAt(address);
          byteVariableReference.SubscribeToChangeNotification(async () =>
          {
            var value = byteVariableReference.Get();
            var payload = BitConverter.GetBytes(value);
            await MqttPublish(payload).ConfigureAwait(false);
          });
          break;

        case "float":
          var floatVariableReference = logo.FloatAt(address);
          floatVariableReference.SubscribeToChangeNotification(async () =>
          {
            var value = floatVariableReference.Get();
            var payload = BitConverter.GetBytes(value);
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
  }
}