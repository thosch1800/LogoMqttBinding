using System;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Byte = LogoMqttBinding.LogoAdapter.Byte;

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

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
      };
    }

    public void PulseLogoVariable(Mqtt.Subscription subscription, int address, MqttChannelConfig.Types type)
    {
      if (type != MqttChannelConfig.Types.Byte)
        throw new ArgumentOutOfRangeException(nameof(type), type, null);

      subscription.MessageReceived += async (sender, args) =>
        await mapping.PulseByte(logo.ByteAt(address), args.ApplicationMessage.Payload);
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



    public async Task PulseByte(Byte logoVariable, byte[] payload)
    {
      if (mqttFormat.ToValue(payload, out byte value))
        logoVariable.Set(value);

      await Task.Delay(250);
      logoVariable.Set(0);
    }



    public async Task PublishInteger(ILogoVariable<short> logoVariable, string topic, bool retain, MqttQualityOfServiceLevel qualityOfService)
    {
      var value = logoVariable.Get();
      await Publish(topic, MqttFormat.ToPayload(value), retain, qualityOfService).ConfigureAwait(false);
    }

    public async Task PublishByte(ILogoVariable<byte> logoVariable, string topic, bool retain, MqttQualityOfServiceLevel qualityOfService)
    {
      var value = logoVariable.Get();
      await Publish(topic, MqttFormat.ToPayload(value), retain, qualityOfService).ConfigureAwait(false);
    }

    public async Task PublishFloat(ILogoVariable<float> logoVariable, string topic, bool retain, MqttQualityOfServiceLevel qualityOfService)
    {
      var value = logoVariable.Get();
      await Publish(topic, MqttFormat.ToPayload(value), retain, qualityOfService).ConfigureAwait(false);
    }



    private async Task Publish(string topic, byte[] payload, bool retain, MqttQualityOfServiceLevel qualityOfService)
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
    private readonly MqttFormat mqttFormat;
  }
}