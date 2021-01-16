using System;
using System.Collections.Generic;
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
          "ClientId should not be empty or whitespace");

      var status = mqttClientConfig.Status;
      if (status is not null)
        ValidateStatusChannel(status);

      foreach (var mqttChannelConfig in mqttClientConfig.Channels)
        ValidateLogoChannel(mqttChannelConfig);
    }

    private static void ValidateLogoChannel(MqttLogoChannelConfig channelConfig)
    {
      if (!EnumIsDefined(typeof(MqttChannelConfigBase.Actions), channelConfig.Action))
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.Action),
          channelConfig.Action,
          $"Allowed values are {string.Join(", ", Enum.GetNames(typeof(MqttChannelConfigBase.Actions)))}");

      var allowedTypes = new[] { MqttChannelConfigBase.Types.Byte, MqttChannelConfigBase.Types.Integer, MqttChannelConfigBase.Types.Float };
      if (!EnumIsDefined(typeof(MqttChannelConfigBase.Types), channelConfig.Type)
          || TypeIsOneOf(channelConfig, allowedTypes))
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.Type),
          channelConfig.Type,
          $"Allowed values are {string.Join(", ", allowedTypes)}");

      if (!EnumIsDefined(typeof(MqttChannelConfigBase.QoS), channelConfig.QualityOfService))
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.QualityOfService),
          channelConfig.QualityOfService,
          $"Allowed values are {string.Join(", ", Enum.GetNames(typeof(MqttChannelConfigBase.QoS)))}");

      if (channelConfig.LogoAddress is < MemoryRangeMinimum or > MemoryRangeMaximum)
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.LogoAddress),
          channelConfig.LogoAddress,
          $"The range should be {MemoryRangeMinimum}..{MemoryRangeMaximum}");

      ValidateTopic(channelConfig.Topic, nameof(channelConfig.Topic));
    }

    private static bool TypeIsOneOf(MqttChannelConfigBase channel, IEnumerable<MqttChannelConfigBase.Types> allowedTypes)
    {
      try
      {
        var type = channel.GetTypeAsEnum();
        return !allowedTypes.Contains(type);
      }
      catch { return true; }
    }

    private static void ValidateStatusChannel(MqttStatusChannelConfig channelConfig)
    {
      if (channelConfig.GetActionAsEnum() != MqttChannelConfigBase.Actions.Publish)
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.Action),
          channelConfig.Action,
          $"Allowed value is {MqttChannelConfigBase.Actions.Publish}");

      if (channelConfig.GetTypeAsEnum() != MqttChannelConfigBase.Types.String)
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.Type),
          channelConfig.Type,
          $"Allowed value is {MqttChannelConfigBase.Types.String}");

      if (channelConfig.GetQualityOfServiceAsEnum() != MqttChannelConfigBase.QoS.ExactlyOnce)
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.QualityOfService),
          channelConfig.QualityOfService,
          $"Allowed value is {MqttChannelConfigBase.QoS.ExactlyOnce}");

      if (channelConfig.Retain is false)
        throw new ArgumentOutOfRangeException(
          nameof(channelConfig.Retain),
          channelConfig.Retain,
          $"Allowed value is {true}");

      ValidateTopic(channelConfig.Topic, nameof(channelConfig.Topic));
    }

    private static void ValidateTopic(string topic, string nameOfTopicProperty)
    {
      if (string.IsNullOrWhiteSpace(topic))
        throw new ArgumentOutOfRangeException(
          nameOfTopicProperty,
          topic,
          "Topic should not be empty or whitespace");

      if (topic.Any(char.IsWhiteSpace))
        throw new ArgumentOutOfRangeException(
          nameOfTopicProperty,
          topic,
          "Topic should not contain whitespace");

      if (topic.StartsWith('/'))
        throw new ArgumentOutOfRangeException(
          nameOfTopicProperty,
          topic,
          "Topic should not start with /");

      if (topic.Contains('#') && !topic.EndsWith('#'))
        throw new ArgumentOutOfRangeException(
          nameOfTopicProperty,
          topic,
          "Topic should contain # at the end only");

      if (topic.Contains("//"))
        throw new ArgumentOutOfRangeException(
          nameOfTopicProperty,
          topic,
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