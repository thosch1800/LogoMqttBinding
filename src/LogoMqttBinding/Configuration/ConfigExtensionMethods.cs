using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace LogoMqttBinding.Configuration
{
  public static class ConfigExtensionMethods
  {
    public static void Read(this Config config, string path)
    {
      new ConfigurationBuilder()
        .AddJsonFile(path, false)
        .Build()
        .Bind(config);
    }

    public static void Validate(this Config config)
    {
      if (!Uri.IsWellFormedUriString(config.MqttBrokerUri, UriKind.RelativeOrAbsolute))
        throw new ArgumentOutOfRangeException(
          nameof(config.MqttBrokerUri),
          config.MqttBrokerUri,
          $"'{config.MqttBrokerUri}' should be a valid URI");

      if (config.MqttBrokerPort is < 0 or > 65535)
        throw new ArgumentOutOfRangeException(
          nameof(config.MqttBrokerPort),
          config.MqttBrokerPort,
          $"'{config.MqttBrokerPort}' should be a valid port");

      foreach (var logoConfig in config.Logos)
      {
        if (!IPAddress.TryParse(logoConfig.IpAddress, out _))
          throw new ArgumentOutOfRangeException(
            nameof(logoConfig.IpAddress),
            logoConfig.IpAddress,
            $"'{logoConfig.IpAddress}' should be a valid IP address");

        foreach (var memoryRangeConfig in logoConfig.MemoryRanges)
          ValidateMemoryRangeConfig(memoryRangeConfig);

        foreach (var mqttClientConfig in logoConfig.Mqtt)
          ValidateMqttClientConfig(mqttClientConfig);
      }
    }

    private static void ValidateMemoryRangeConfig(MemoryRangeConfig memoryRangeConfig)
    {
      if (memoryRangeConfig.LocalVariableMemoryStart is < MemoryRangeMinimum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRangeConfig.LocalVariableMemoryStart),
          memoryRangeConfig.LocalVariableMemoryStart,
          $"The range should be {MemoryRangeMinimum}..{MemoryRangeMaximum}");

      if (memoryRangeConfig.LocalVariableMemoryEnd is > MemoryRangeMaximum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRangeConfig.LocalVariableMemoryEnd),
          memoryRangeConfig.LocalVariableMemoryEnd,
          $"The range should be {MemoryRangeMinimum}..{MemoryRangeMaximum}");

      if (memoryRangeConfig.LocalVariableMemorySize is < 1 or > MemoryRangeMaximum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRangeConfig.LocalVariableMemorySize),
          memoryRangeConfig.LocalVariableMemorySize,
          $"Size should be 1..{MemoryRangeMaximum}");

      if (memoryRangeConfig.LocalVariableMemoryPollingCycleMilliseconds is < PollingCycleMillisecondsMinimum)
        throw new ArgumentOutOfRangeException(
          nameof(memoryRangeConfig.LocalVariableMemoryPollingCycleMilliseconds),
          memoryRangeConfig.LocalVariableMemoryPollingCycleMilliseconds,
          $"Polling cycle should be greater than {PollingCycleMillisecondsMinimum}");
    }

    private static void ValidateMqttClientConfig(MqttClientConfig mqttClientConfig)
    {
      if (string.IsNullOrWhiteSpace(mqttClientConfig.ClientId))
        throw new ArgumentOutOfRangeException(
          nameof(mqttClientConfig.ClientId),
          mqttClientConfig.ClientId,
          $"ClientId should not be empty or whitespace");

      var lastWill = mqttClientConfig.LastWill;
      if (lastWill is not null)
      {
        if (!EnumIsDefined(typeof(MqttChannelConfig.Actions), lastWill.Action) ||
            lastWill.GetActionAsEnum() != MqttChannelConfig.Actions.Publish)
          throw new ArgumentOutOfRangeException(
            nameof(mqttClientConfig.LastWill) + "." + nameof(lastWill.Action),
            lastWill.Action,
            $"LastWill should provide action publish");

        if (string.IsNullOrWhiteSpace(lastWill.Payload))
          throw new ArgumentOutOfRangeException(
            nameof(mqttClientConfig.LastWill) + "." + nameof(lastWill.Payload),
            lastWill.Payload,
            $"LastWill should provide a payload");

        ValidateTopic(lastWill);
      }

      foreach (var mqttChannelConfig in mqttClientConfig.Channels)
        ValidateChannel(mqttChannelConfig);
    }

    private static void ValidateChannel(MqttChannelConfig mqttChannelConfig)
    {
      if (!EnumIsDefined(typeof(MqttChannelConfig.Actions), mqttChannelConfig.Action))
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.Action),
          mqttChannelConfig.Action,
          $"Allowed values are {string.Join(", ", Enum.GetNames(typeof(MqttChannelConfig.Actions)))}");

      if (!EnumIsDefined(typeof(MqttChannelConfig.Types), mqttChannelConfig.Type))
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.Type),
          mqttChannelConfig.Type,
          $"Allowed values are {string.Join(", ", Enum.GetNames(typeof(MqttChannelConfig.Types)))}");

      if (mqttChannelConfig.LogoAddress is < MemoryRangeMinimum or > MemoryRangeMaximum)
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.LogoAddress),
          mqttChannelConfig.LogoAddress,
          $"The range should be {MemoryRangeMinimum}..{MemoryRangeMaximum}");

      ValidateTopic(mqttChannelConfig);
    }

    private static void ValidateTopic(MqttChannelConfig mqttChannelConfig)
    {
      if (string.IsNullOrWhiteSpace(mqttChannelConfig.Topic))
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.Topic),
          mqttChannelConfig.Topic,
          "Topic should not be empty or whitespace");

      if (mqttChannelConfig.Topic.Any(char.IsWhiteSpace))
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.Topic),
          mqttChannelConfig.Topic,
          "Topic should not contain whitespace");

      if (mqttChannelConfig.Topic.StartsWith('/'))
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.Topic),
          mqttChannelConfig.Topic,
          "Topic should not start with /");

      if (mqttChannelConfig.Topic.Contains('#') && !mqttChannelConfig.Topic.EndsWith('#'))
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.Topic),
          mqttChannelConfig.Topic,
          "Topic should contain # at the end only");

      if (mqttChannelConfig.Topic.Contains("//"))
        throw new ArgumentOutOfRangeException(
          nameof(mqttChannelConfig.Topic),
          mqttChannelConfig.Topic,
          "Topic should not define empty groups");
    }

    private static bool EnumIsDefined(Type type, string value)
      => Enum.TryParse(type, value, true, out var action) &&
         Enum.IsDefined(type, action!);

    private const int MemoryRangeMinimum = 0;
    private const int MemoryRangeMaximum = 850;
    private const int PollingCycleMillisecondsMinimum = 100;
  }
}