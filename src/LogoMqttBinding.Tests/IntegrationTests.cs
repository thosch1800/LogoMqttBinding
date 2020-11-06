using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.Tests.Infrastructure;
using MQTTnet;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using Xunit;

namespace LogoMqttBinding.Tests
{
  [Collection(nameof(IntegrationTestEnvironment))]
  public class IntegrationTests
  {
    private readonly IntegrationTestEnvironment testEnvironment;
    public IntegrationTests(IntegrationTestEnvironment testEnvironment) => this.testEnvironment = testEnvironment;



    [Theory]
    [InlineData(0, "get/integer/at/0", 7)]
    [InlineData(17, "get/integer/at/17", 1337)]
    public async Task ChangedValueInLogo_Integer_TriggersMqttWithCorrectValue(int logoAddress, string mqttTopic, short value)
    {
      MqttApplicationMessageReceivedEventArgs? receivedMessage = null;
      testEnvironment.MqttMessageReceived += (s, e) => receivedMessage = e;

      await testEnvironment.MqttClient!.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(mqttTopic).Build(), CancellationToken.None);

      testEnvironment.LogoHardwareMock!.WriteInteger(logoAddress, value);
      await Task.Delay(250).ConfigureAwait(false); // let cache update, detect change and publish

      await testEnvironment.MqttClient.UnsubscribeAsync(new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(mqttTopic).Build(), CancellationToken.None);

      receivedMessage.Should().NotBeNull();
      var receivedString = Encoding.UTF8.GetString(receivedMessage!.ApplicationMessage.Payload);
      var actualValue = short.Parse(receivedString);
      actualValue.Should().Be(value);
    }

    [Theory]
    [InlineData(5, "set/integer/at/5", 7)]
    [InlineData(25, "set/integer/at/25", 1337)]
    public async Task SetValueFromMqtt_Integer_UpdatesLogoWithCorrectValue(int logoAddress, string mqttTopic, short value)
    {
      var payload = Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture));

      await testEnvironment.MqttClient!.PublishAsync(
        new MqttApplicationMessageBuilder()
          .WithTopic(mqttTopic)
          .WithPayload(payload)
          .Build(),
        CancellationToken.None);

      await Task.Delay(100).ConfigureAwait(false);

      var actualValue = testEnvironment.LogoHardwareMock!.ReadInteger(logoAddress);

      actualValue.Should().Be(value);
    }



    [Theory]
    [InlineData(100, "get/float/at/100", 42)]
    [InlineData(105, "get/float/at/105", 13.3f)]
    public async Task ChangedValueInLogo_Float_TriggersMqttWithCorrectValue(int logoAddress, string mqttTopic, short value)
    {
      MqttApplicationMessageReceivedEventArgs? receivedMessage = null;
      testEnvironment.MqttMessageReceived += (s, e) => receivedMessage = e;

      await testEnvironment.MqttClient!.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(mqttTopic).Build(), CancellationToken.None);

      testEnvironment.LogoHardwareMock!.WriteFloat(logoAddress, value);
      await Task.Delay(250).ConfigureAwait(false); // let cache update, detect change and publish

      await testEnvironment.MqttClient.UnsubscribeAsync(new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(mqttTopic).Build(), CancellationToken.None);

      receivedMessage.Should().NotBeNull();
      var receivedString = Encoding.UTF8.GetString(receivedMessage!.ApplicationMessage.Payload);
      var actualValue = float.Parse(receivedString);
      actualValue.Should().Be(value);
    }

    [Theory]
    [InlineData(100, "set/float/at/100", 23)]
    [InlineData(105, "set/float/at/105", 123.0f)]
    public async Task SetValueFromMqtt_Float_UpdatesLogoWithCorrectValue(int logoAddress, string mqttTopic, float value)
    {
      var payload = Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture));

      await testEnvironment.MqttClient!.PublishAsync(
        new MqttApplicationMessageBuilder()
          .WithTopic(mqttTopic)
          .WithPayload(payload)
          .Build(),
        CancellationToken.None);

      await Task.Delay(100).ConfigureAwait(false);

      var actualValue = testEnvironment.LogoHardwareMock!.ReadFloat(logoAddress);

      actualValue.Should().Be(value);
    }



    [Theory]
    [InlineData(200, "get/byte/at/200", 42)]
    [InlineData(205, "get/byte/at/205", 128)]
    public async Task ChangedValueInLogo_Byte_TriggersMqttWithCorrectValue(int logoAddress, string mqttTopic, byte value)
    {
      MqttApplicationMessageReceivedEventArgs? receivedMessage = null;
      testEnvironment.MqttMessageReceived += (s, e) => receivedMessage = e;

      await testEnvironment.MqttClient!.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(mqttTopic).Build(), CancellationToken.None);

      testEnvironment.LogoHardwareMock!.WriteByte(logoAddress, value);
      await Task.Delay(250).ConfigureAwait(false); // let cache update, detect change and publish

      await testEnvironment.MqttClient.UnsubscribeAsync(new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(mqttTopic).Build(), CancellationToken.None);

      receivedMessage.Should().NotBeNull();
      var receivedString = Encoding.UTF8.GetString(receivedMessage!.ApplicationMessage.Payload);
      var actualValue = byte.Parse(receivedString);
      actualValue.Should().Be(value);
    }

    [Theory]
    [InlineData(200, "set/byte/at/200", 23)]
    [InlineData(205, "set/byte/at/205", 222)]
    public async Task SetValueFromMqtt_Byte_UpdatesLogoWithCorrectValue(int logoAddress, string mqttTopic, byte value)
    {
      var payload = Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture));

      await testEnvironment.MqttClient!.PublishAsync(
        new MqttApplicationMessageBuilder()
          .WithTopic(mqttTopic)
          .WithPayload(payload)
          .Build(),
        CancellationToken.None);

      await Task.Delay(100).ConfigureAwait(false);

      var actualValue = testEnvironment.LogoHardwareMock!.ReadByte(logoAddress);

      actualValue.Should().Be(value);
    }



    //TODO: test: subscribe / connect / enforce reconnect -> change value should only notify once

    internal static Config GetConfig(string brokerUri, int brokerPort)
    {
      return new Config
      {
        MqttBrokerUri = brokerUri,
        MqttBrokerPort = brokerPort,
        Logos = new[]
        {
          new LogoConfig
          {
            IpAddress = IPAddress.Loopback.ToString(),
            MemoryRanges = new[]
            {
              new MemoryRangeConfig
              {
                LocalVariableMemoryStart = 0,
                LocalVariableMemoryEnd = 850,
                LocalVariableMemoryPollingCycleMilliseconds = 100,
              },
            },
            Mqtt = new[]
            {
              new MqttClient
              {
                ClientId = "mqttClient",

                Subscribe = new[]
                {
                  new MqttChannel
                  {
                    Topic = "set/integer/at/5",
                    LogoAddress = 5,
                    Type = "integer",
                  },
                  new MqttChannel
                  {
                    Topic = "set/integer/at/25",
                    LogoAddress = 25,
                    Type = "integer",
                  },

                  new MqttChannel
                  {
                    Topic = "set/float/at/100",
                    LogoAddress = 100,
                    Type = "float",
                  },
                  new MqttChannel
                  {
                    Topic = "set/float/at/105",
                    LogoAddress = 105,
                    Type = "float",
                  },

                  new MqttChannel
                  {
                    Topic = "set/byte/at/200",
                    LogoAddress = 200,
                    Type = "byte",
                  },
                  new MqttChannel
                  {
                    Topic = "set/byte/at/205",
                    LogoAddress = 205,
                    Type = "byte",
                  },
                },

                Publish = new[]
                {
                  new MqttChannel
                  {
                    Topic = "get/integer/at/0",
                    LogoAddress = 0,
                    Type = "integer",
                  },
                  new MqttChannel
                  {
                    Topic = "get/integer/at/17",
                    LogoAddress = 17,
                    Type = "integer",
                  },

                  new MqttChannel
                  {
                    Topic = "get/float/at/100",
                    LogoAddress = 100,
                    Type = "float",
                  },
                  new MqttChannel
                  {
                    Topic = "get/float/at/105",
                    LogoAddress = 105,
                    Type = "float",
                  },

                  new MqttChannel
                  {
                    Topic = "get/byte/at/200",
                    LogoAddress = 200,
                    Type = "byte",
                  },
                  new MqttChannel
                  {
                    Topic = "get/byte/at/205",
                    LogoAddress = 205,
                    Type = "byte",
                  },
                },
              },
            },
          },
        },
      };
    }
  }
}