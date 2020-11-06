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
            config.MqttBrokerUsername,
            config.MqttBrokerPassword);
          mqttClients.Add(mqttClient);

          var mapper = new Mapper(loggerFactory, logo, mqttClient);

          foreach (var subscribed in mqttClientConfig.Subscribe)
          {
            logger.LogInformation($"-- subscribe {subscribed.Topic} (@{subscribed.LogoAddress}[{subscribed.Type}])");
            mapper.WriteLogoVariable(mqttClient.Subscribe(subscribed.Topic), subscribed.LogoAddress, subscribed.Type);
          }

          foreach (var published in mqttClientConfig.Publish)
          {
            logger.LogInformation($"-- publish {published.Topic} (@{published.LogoAddress}[{published.Type}])");
            mapper.PublishOnChange(published.Topic, published.LogoAddress, published.Type);
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