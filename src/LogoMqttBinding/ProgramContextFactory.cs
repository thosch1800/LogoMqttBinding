using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;

namespace LogoMqttBinding
{
  internal class ProgramContextFactory
  {
    private readonly ILoggerFactory loggerFactory;

    public ProgramContextFactory(ILoggerFactory loggerFactory)
    {
      this.loggerFactory = loggerFactory;
      logger = loggerFactory.CreateLogger(nameof(ProgramContextFactory));
    }

    internal ProgramContext Initialize(Config config)
    {
      logger.LogInformation($"MQTT broker at {config.MqttBrokerUri} port {config.MqttBrokerPort} user {config.MqttBrokerUsername}");

      InitializeLogos( config);

      return new ProgramContext(
        loggerFactory.CreateLogger<ProgramContext>(),
        logos.ToImmutableArray(),
        mqttClients.ToImmutableArray());
    }

    private void InitializeLogos(Config config)
    {
      foreach (var logoConfig in config.Logos)
      {
        logger.LogInformation($"Logo PLC at {logoConfig.IpAddress}");

        var logo = new Logo(
          loggerFactory.CreateLogger<Logo>(),
          logoConfig.IpAddress,
          logoConfig.MemoryRanges);

        foreach (var mqttClientConfig in logoConfig.Mqtt)
        {
          logger.LogInformation($"- MQTT client {mqttClientConfig.ClientId} status channel {mqttClientConfig.Status}");

          var mqttClient = new Mqtt(
            loggerFactory.CreateLogger<Mqtt>(),
            mqttClientConfig.ClientId,
            config.MqttBrokerUri,
            config.MqttBrokerPort,
            mqttClientConfig.CleanSession,
            config.MqttBrokerUsername,
            config.MqttBrokerPassword,
            mqttClientConfig.Status);

          InitializeChannels(
            mqttClientConfig, 
            new Mapper(loggerFactory, logo, mqttClient),
            mqttClient);

          mqttClients.Add(mqttClient);
        }

        logos.Add(logo);
      }
    }

    private void InitializeChannels(MqttClientConfig mqttClientConfig, Mapper mapper, Mqtt mqttClient)
    {
      foreach (var channel in mqttClientConfig.Channels)
      {
        var action = channel.GetActionAsEnum();

        logger.LogInformation($"-- {action} {channel.Topic} QoS:{(int) channel.GetQualityOfServiceAsEnum()}/{channel.GetQualityOfServiceAsEnum()} retain:{channel.Retain} logo:{channel.Type}@{channel.LogoAddress}");

        switch (action)
        {
          case MqttChannelConfigBase.Actions.Publish:
            mapper.PublishOnChange(
              channel.Topic,
              channel.LogoAddress,
              channel.GetTypeAsEnum(),
              channel.Retain,
              channel.GetQualityOfServiceAsEnum().ToMqttNet());
            break;

          case MqttChannelConfigBase.Actions.Subscribe:
            mapper.WriteLogoVariable(
              mqttClient.Subscribe(
                channel.Topic,
                channel.GetQualityOfServiceAsEnum().ToMqttNet()),
              channel.LogoAddress,
              channel.GetTypeAsEnum());
            break;

          case MqttChannelConfigBase.Actions.SubscribePulse:
            mapper.PulseLogoVariable(
              mqttClient.Subscribe(
                channel.Topic,
                channel.GetQualityOfServiceAsEnum().ToMqttNet()),
              channel.LogoAddress,
              channel.GetTypeAsEnum(),
              channel.Duration);
            break;

          default: throw new ArgumentOutOfRangeException();
        }
      }
    }

    private readonly ILogger logger;
    private readonly List<Logo> logos = new();
    private readonly List<Mqtt> mqttClients = new();
  }
}