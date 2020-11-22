using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.LogoAdapter.Interfaces;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace LogoMqttBinding
{
  internal class Mapper
  {
    public Mapper(ILoggerFactory loggerFactory, Logo logo, Mqtt mqttClient)
    {
      this.logo = logo;
      mapping = new Mapping(loggerFactory, mqttClient);
    }

    public void WriteLogoVariable(Mqtt.Subscription subscription, int address, MqttChannelConfig.Types type)
    {
      subscription.MessageReceived += type switch
      {
        MqttChannelConfig.Types.Integer => (sender, args) =>
          mapping.ReceivedInteger(logo.IntegerAt(address), args.ApplicationMessage.Payload),

        MqttChannelConfig.Types.Byte => (sender, args) =>
          mapping.ReceivedByte(logo.ByteAt(address), args.ApplicationMessage.Payload),

        MqttChannelConfig.Types.Float => (sender, args) =>
          mapping.ReceivedFloat(logo.FloatAt(address), args.ApplicationMessage.Payload),

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
      };
    }

    public void PulseLogoVariable(Mqtt.Subscription subscription, int address, MqttChannelConfig.Types type, int duration)
    {
      if (type != MqttChannelConfig.Types.Byte)
        throw new ArgumentOutOfRangeException(nameof(type), type, null);

      subscription.MessageReceived += async (sender, args) =>
        await mapping.PulseByte(logo.ByteAt(address), args.ApplicationMessage.Payload, duration);
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public NotificationContext PublishOnChange(string topic, int address, MqttChannelConfig.Types type, bool retain, MqttQualityOfServiceLevel qualityOfService)
    {
      return type switch
      {
        MqttChannelConfig.Types.Integer =>
          logo
            .IntegerAt(address)
            .SubscribeToChangeNotification(
              async logoVariable =>
                await mapping
                  .PublishInteger(logoVariable, topic, retain, qualityOfService)
                  .ConfigureAwait(false)),

        MqttChannelConfig.Types.Byte =>
          logo
            .ByteAt(address)
            .SubscribeToChangeNotification(
              async logoVariable =>
                await mapping
                  .PublishByte(logoVariable, topic, retain, qualityOfService)
                  .ConfigureAwait(false)),

        MqttChannelConfig.Types.Float =>
          logo
            .FloatAt(address)
            .SubscribeToChangeNotification(
              async logoVariable =>
                await mapping
                  .PublishFloat(logoVariable, topic, retain, qualityOfService)
                  .ConfigureAwait(false)),

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "should be integer, byte or float"),
      };
    }

    private readonly Logo logo;
    private readonly Mapping mapping;
  }



  internal class Mapping
  {
    public Mapping(ILoggerFactory loggerFactory, Mqtt mqttClient)
    {
      this.mqttClient = mqttClient;
      logger = loggerFactory.CreateLogger<Mapping>();
    }



    public void ReceivedInteger(ILogoVariable<short> logoVariable, byte[]? payload)
    {
      if (MqttFormat.ToValue(payload, out short value))
        LogoSet(logoVariable, value);
      else
        PrintWarning(logoVariable, payload);
    }

    public void ReceivedByte(ILogoVariable<byte> logoVariable, byte[]? payload)
    {
      if (MqttFormat.ToValue(payload, out byte value))
        LogoSet(logoVariable, value);
      else
        PrintWarning(logoVariable, payload);
    }

    public void ReceivedFloat(ILogoVariable<float> logoVariable, byte[]? payload)
    {
      if (MqttFormat.ToValue(payload, out float value))
        LogoSet(logoVariable, value);
      else
        PrintWarning(logoVariable, payload);
    }

    public async Task PulseByte(LogoAdapter.Byte logoVariable, byte[]? payload, int duration)
    {
      if (MqttFormat.ToValue(payload, out byte value))
      {
        LogoSet(logoVariable, value);
        await Task.Delay(duration);
        LogoSet<byte>(logoVariable, 0);
      }
      else
        PrintWarning(logoVariable, payload);
    }



    public async Task PublishInteger(ILogoVariable<short> logoVariable, string topic, bool retain, MqttQualityOfServiceLevel qualityOfService)
    {
      var value = logoVariable.Get();
      logger.LogDebug($"{logoVariable} changed to {value}");
      await MqttPublish(topic, MqttFormat.ToPayload(value), qualityOfService, retain).ConfigureAwait(false);
    }

    public async Task PublishByte(ILogoVariable<byte> logoVariable, string topic, bool retain, MqttQualityOfServiceLevel qualityOfService)
    {
      var value = logoVariable.Get();
      logger.LogDebug($"{logoVariable} changed to {value}");
      await MqttPublish(topic, MqttFormat.ToPayload(value), qualityOfService, retain).ConfigureAwait(false);
    }

    public async Task PublishFloat(ILogoVariable<float> logoVariable, string topic, bool retain, MqttQualityOfServiceLevel qualityOfService)
    {
      var value = logoVariable.Get();
      logger.LogDebug($"{logoVariable} changed to {value}");
      await MqttPublish(topic, MqttFormat.ToPayload(value), qualityOfService, retain).ConfigureAwait(false);
    }



    private void LogoSet<T>(ILogoVariable<T> logoVariable, T value)
    {
      logger.LogDebug($"{logoVariable} setting to '{value}'");
      logoVariable.Set(value);
    }

    private void PrintWarning<T>(ILogoVariable<T> logoVariable, IEnumerable<byte>? payload)
    {
      var payloadString = payload != null ? string.Join("-", payload.Select(b => b.ToString("X"))) : "null";
      logger.LogWarning($"{logoVariable} failed to set payload '{payloadString}'");
    }


    private async Task MqttPublish(string topic, byte[] payload, MqttQualityOfServiceLevel qualityOfService, bool retain)
    {
      await mqttClient
        .PublishAsync(new MqttApplicationMessageBuilder()
          .WithTopic(topic)
          .WithPayload(payload)
          .WithRetainFlag(retain)
          .WithQualityOfServiceLevel(qualityOfService)
          .Build())
        .ConfigureAwait(false);
    }

    private readonly Mqtt mqttClient;
    private readonly ILogger<Mapping> logger;
  }
}