using System.Collections.Generic;
using System.Collections.Immutable;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;

namespace LogoMqttBinding
{
  internal static class Logic
  {
    internal static ProgramContext Initialize(ILoggerFactory loggerFactory, Config config)
    {
      var logger = loggerFactory.CreateLogger(nameof(Logic));
      var logos = new List<Logo>();
      var mqttClients = new List<Mqtt>();

      logger.LogInformation($"MQTT broker {config.MqttBrokerUri}:{config.MqttBrokerPort}");

      foreach (var logoConfig in config.Logos)
      {
        logger.LogInformation($"Logo at {logoConfig.IpAddress}");

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
            config.MqttBrokerUsername,
            config.MqttBrokerPassword);
          mqttClients.Add(mqttClient);

          foreach (var subscribed in mqttClientConfig.Subscribe)
          {
            logger.LogInformation($"-- subscribe {subscribed.Topic} (@{subscribed.LogoAddress}[{subscribed.Type}])");

            mqttClient
              .Subscribe(subscribed.Topic)
              .AddLogoSetValueHandler(logo, subscribed.Type, subscribed.LogoAddress);
          }

          foreach (var published in mqttClientConfig.Publish)
          {
            logger.LogInformation($"-- publish {published.Topic} (@{published.LogoAddress}[{published.Type}])");

            LogoMqttMapping
              .LogoNotifyOnChange(logo, mqttClient, published.Type, published.Topic, published.LogoAddress);
            /*
            mqttClient
              .Subscribe(published.Topic)
              .AddLogoGetValueHandler(logo, mqttClient, published.Type, published.Topic, published.LogoAddress);
          */
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