using System;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace LogoMqttBinding.Configuration
{
  public static class ConfigExtensionMethods
  {
    public static void Read(this Config config, string path = "./config/logo-mqtt.json")
    {
      new ConfigurationBuilder()
        .AddJsonFile(path, false)
        .Build()
        .Bind(config);
    }

    private const int MemoryRangeMinimum = 0;
    private const int MemoryRangeMaximum = 850;
    private const int PollingCycleMillisecondsMinimum = 100;

    public static void Validate(this Config config)
    {
      if (!Uri.IsWellFormedUriString(config.MqttBrokerIpAddress, UriKind.RelativeOrAbsolute))
        throw new ArgumentOutOfRangeException(
          nameof(config.MqttBrokerIpAddress),
          config.MqttBrokerIpAddress,
          $"'{config.MqttBrokerIpAddress}' should be a valid URI");

      if (config.MqttBrokerPort is < 0 or > 65535)
        throw new ArgumentOutOfRangeException(
          nameof(config.MqttBrokerPort),
          config.MqttBrokerPort,
          $"'{config.MqttBrokerPort}' should be a valid port");

      foreach (var logo in config.Logos)
      {
        if (!IPAddress.TryParse(logo.IpAddress, out _))
          throw new ArgumentOutOfRangeException(
            nameof(logo.IpAddress),
            logo.IpAddress,
            $"'{logo.IpAddress}' should be a valid IP address");

        foreach (var memoryRange in logo.MemoryRanges)
          ValidateMemoryRange(memoryRange);

        foreach (var mqtt in logo.Mqtt)
          ValidateMqtt(mqtt);
      }
    }

    private static void ValidateMemoryRange(MemoryRangeConfig memoryRange)
    {
      if (memoryRange.LocalVariableMemoryStart is < MemoryRangeMinimum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRange.LocalVariableMemoryStart),
          memoryRange.LocalVariableMemoryStart,
          $"The range should be {MemoryRangeMinimum}..{MemoryRangeMaximum}");

      if (memoryRange.LocalVariableMemoryEnd is > MemoryRangeMaximum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRange.LocalVariableMemoryEnd),
          memoryRange.LocalVariableMemoryEnd,
          $"The range should be {MemoryRangeMinimum}..{MemoryRangeMaximum}");

      if (memoryRange.LocalVariableMemorySize is < 1 or > MemoryRangeMaximum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRange.LocalVariableMemorySize),
          memoryRange.LocalVariableMemorySize,
          $"Size should be 1..{MemoryRangeMaximum}");

      if (memoryRange.LocalVariableMemoryPollingCycleMilliseconds is < PollingCycleMillisecondsMinimum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRange.LocalVariableMemoryPollingCycleMilliseconds),
          memoryRange.LocalVariableMemoryPollingCycleMilliseconds,
          $"Polling cycle should be greater than {PollingCycleMillisecondsMinimum}");
    }

    private static void ValidateMqtt(MqttDevice mqtt)
    {
      foreach (var published in mqtt.Published)
        ValidateLogoAddress(published);

      foreach (var subscribed in mqtt.Subscribed)
        ValidateLogoAddress(subscribed);

      static void ValidateLogoAddress(MqttChannel mqttChannel)
      {
        if (mqttChannel.LogoAddress is < MemoryRangeMinimum or > MemoryRangeMaximum)
          throw new ArgumentOutOfRangeException(
            nameof(mqttChannel.LogoAddress),
            mqttChannel.LogoAddress,
            $"The range should be {MemoryRangeMinimum}..{MemoryRangeMaximum}");
      }
    }
  }
}