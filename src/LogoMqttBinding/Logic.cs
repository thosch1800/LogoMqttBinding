﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;

namespace LogoMqttBinding
{
  //todo v2.0: provide application state as dedicated mqtt client (e.g. log level, statistics, event log)
  internal static class Logic
  {
    internal static ProgramContext Initialize(ILoggerFactory loggerFactory, Config config)
    {
      var logger = loggerFactory.CreateLogger(nameof(Logic));
      var mqttClients = new List<Mqtt>();
      var logos = new List<Logo>();

      logger.LogInformation($"MQTT broker at {config.MqttBrokerUri} using port {config.MqttBrokerPort}");

      foreach (var logoConfig in config.Logos)
      {
        logger.LogInformation($"Logo PLC at {logoConfig.IpAddress}");

        var logo = new Logo(
          loggerFactory.CreateLogger<Logo>(),
          logoConfig.IpAddress,
          logoConfig.MemoryRanges);
        logos.Add(logo);

        foreach (var mqttClientConfig in logoConfig.Mqtt)
        {
          logger.LogInformation($"- MQTT client {mqttClientConfig.ClientId}");

          var mqttClient = new Mqtt(
            loggerFactory.CreateLogger<Mqtt>(),
            mqttClientConfig.ClientId,
            config.MqttBrokerUri,
            config.MqttBrokerPort,
            mqttClientConfig.CleanSession,
            config.MqttBrokerUsername,
            config.MqttBrokerPassword,
            mqttClientConfig.Status);
          mqttClients.Add(mqttClient);

          var mapper = new Mapper(loggerFactory, logo, mqttClient);

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
      }

      return new ProgramContext(
        loggerFactory.CreateLogger<ProgramContext>(),
        logos.ToImmutableArray(),
        mqttClients.ToImmutableArray());
    }
  }
}