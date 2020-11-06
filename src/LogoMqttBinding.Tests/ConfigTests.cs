using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using LogoMqttBinding.Configuration;
using Xunit;

namespace LogoMqttBinding.Tests
{
  public class ConfigTests
  {
    [Fact]
    public void Validate_Uninitialized_ShouldPass()
    {
      var config = new Config();
      config.Validate();
    }

    [Fact]
    public void Validate_InvalidBrokerUri_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""MqttBrokerUri"": ""some\\where"",
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(Config.MqttBrokerUri));
      ex.ActualValue.Should().Be("some\\where");
      ex.Message.Should().Contain("'some\\where' should be a valid URI");
    }

    [Fact]
    public void Validate_BrokerPortBelowRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""MqttBrokerPort"": ""-1"",
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(Config.MqttBrokerPort));
      ex.ActualValue.Should().Be(-1);
      ex.Message.Should().Contain("'-1' should be a valid port");
    }

    [Fact]
    public void Validate_BrokerPortAboveRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""MqttBrokerPort"": ""65536"",
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(Config.MqttBrokerPort));
      ex.ActualValue.Should().Be(65536);
      ex.Message.Should().Contain("'65536' should be a valid port");
    }

    [Fact]
    public void Validate_InvalidLogoIpAddress_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""IpAddress"": ""1.2.3.4.5"",
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(LogoConfig.IpAddress));
      ex.ActualValue.Should().Be("1.2.3.4.5");
      ex.Message.Should().Contain("'1.2.3.4.5' should be a valid IP address");
    }

    [Fact]
    public void Validate_MemoryRangeStartBelowRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""MemoryRanges"": [
        {
          ""LocalVariableMemoryStart"": -1,
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MemoryRangeConfig.LocalVariableMemoryStart));
      ex.ActualValue.Should().Be(-1);
      ex.Message.Should().Contain("0..850");
    }

    [Fact]
    public void Validate_MemoryRangeEndAboveRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""MemoryRanges"": [
        {
          ""LocalVariableMemoryEnd"": 851
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MemoryRangeConfig.LocalVariableMemoryEnd));
      ex.ActualValue.Should().Be(851);
      ex.Message.Should().Contain("0..850");
    }

    [Fact]
    public void Validate_MemoryRangeSizeSmallerOne_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""MemoryRanges"": [
        {
          ""LocalVariableMemoryStart"": 0,
          ""LocalVariableMemoryEnd"": 0
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MemoryRangeConfig.LocalVariableMemorySize));
      ex.ActualValue.Should().Be(0);
      ex.Message.Should().Contain("1..850");
    }

    [Fact]
    public void Validate_MemoryRangePollingCycleSmaller100_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""MemoryRanges"": [
        {
          ""LocalVariableMemoryPollingCycleMilliseconds"": 99,
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MemoryRangeConfig.LocalVariableMemoryPollingCycleMilliseconds));
      ex.ActualValue.Should().Be(99);
      ex.Message.Should().Contain("Polling cycle should be greater than 100");
    }

    [Fact]
    public void Validate_PublishedLogoAddressAboveRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""Mqtt"": [
        {
          ""Channels"": [
            {
              ""LogoAddress"": 0,
            },
            {
              ""LogoAddress"": 851,
            }
          ]
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MqttChannelConfig.LogoAddress));
      ex.ActualValue.Should().Be(851);
      ex.Message.Should().Contain("should be 0..850");
    }

    [Fact]
    public void Validate_PublishedLogoAddressBelowRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""Mqtt"": [
        {
          ""Channels"": [
            {
              ""LogoAddress"": 0,
            },
            {
              ""LogoAddress"": -1,
            }
          ]
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MqttChannelConfig.LogoAddress));
      ex.ActualValue.Should().Be(-1);
      ex.Message.Should().Contain("should be 0..850");
    }

    [Fact]
    public void Validate_SubscribedLogoAddressAboveRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""Mqtt"": [
        {
          ""Channels"": [
            {
              ""LogoAddress"": 851,
            },
            {
              ""LogoAddress"": 42,
            }
          ]
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MqttChannelConfig.LogoAddress));
      ex.ActualValue.Should().Be(851);
      ex.Message.Should().Contain("should be 0..850");
    }

    [Fact]
    public void Validate_SubscribedLogoAddressBelowRange_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""Mqtt"": [
        {
          ""Channels"": [
            {
              ""LogoAddress"": -1,
            },
            {
              ""LogoAddress"": 42,
            }
          ]
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MqttChannelConfig.LogoAddress));
      ex.ActualValue.Should().Be(-1);
      ex.Message.Should().Contain("should be 0..850");
    }

    [Fact]
    public void Validate_InvalidAction_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""Mqtt"": [
        {
          ""Channels"": [
            {
              ""Action"": ""someUndefinedValue"",
            }
          ]
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MqttChannelConfig.Action));
      ex.ActualValue.Should().Be("someUndefinedValue");
      ex.Message.Should().Contain("Allowed values are Publish, SubscribeSet, SubscribePulse");
    }

    [Fact]
    public void Validate_InvalidType_ShouldThrow()
    {
      using var configFile = new TempFile(@"
{
  ""Logos"": [
    {
      ""Mqtt"": [
        {
          ""Channels"": [
            {
              ""Type"": ""someUndefinedType""
            }
          ]
        }
      ]
    }
  ]
}");
      var config = new Config();
      config.Read(configFile.Path);

      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
      ex.ParamName.Should().Be(nameof(MqttChannelConfig.Type));
      ex.ActualValue.Should().Be("someUndefinedType");
      ex.Message.Should().Contain("Allowed values are Byte, Integer, Float");
    }
    
    
    
    

    [Fact]
    public void Read_ValidContent_Succeeds()
    {
      using var configFile = new TempFile(@"
{
  ""MqttBrokerUri"": ""5.6.7.8"",
  ""MqttBrokerPort"": ""6667"",
  ""MqttBrokerUsername"": ""mqttUsername"",
  ""MqttBrokerPassword"": ""mqttPasswd"",
  ""Logos"": [
    {
      ""IpAddress"": ""1.2.3.4"",
      ""MemoryRanges"": [
        {
          ""LocalVariableMemoryPollingCycleMilliseconds"": 10000,
          ""LocalVariableMemoryStart"": 0,
          ""LocalVariableMemoryEnd"": 128
        },
        {
          ""LocalVariableMemoryPollingCycleMilliseconds"": 666,
          ""LocalVariableMemoryStart"": 12,
          ""LocalVariableMemoryEnd"": 42
        }
      ],
      ""Mqtt"": [
        {
          ""ClientId"": ""mqtt-client-id"",
          ""Channels"": [
            {
              ""Action"": ""subscribeSet"",
              ""Topic"": ""map/21/31/set"",
              ""LogoAddress"": 21,
              ""Type"": ""byte""
            },
            {
              ""Action"": ""subscribeSet"",
              ""Topic"": ""map/22/32/set"",
              ""LogoAddress"": 22,
              ""Type"": ""integer""
            },
            {
              ""Action"": ""subscribeSet"",
              ""Topic"": ""map/26/36/set"",
              ""LogoAddress"": 26,
              ""Type"": ""float""
            },
            {
              ""Action"": ""publish"",
              ""Topic"": ""map/121/131/get"",
              ""LogoAddress"": 121,
              ""Type"": ""byte""
            },
            {
              ""Action"": ""publish"",
              ""Topic"": ""map/122/132/get"",
              ""LogoAddress"": 122,
              ""Type"": ""integer""
            },
            {
              ""Action"": ""publish"",
              ""Topic"": ""map/126/136/get"",
              ""LogoAddress"": 126,
              ""Type"": ""float""
            }
          ]
        }
      ]
    }
  ]
}");

      var config = new Config();
      config.Read(configFile.Path);

      config.MqttBrokerUri.Should().Be("5.6.7.8");
      config.MqttBrokerPort.Should().Be(6667);
      config.MqttBrokerUsername.Should().Be("mqttUsername");
      config.MqttBrokerPassword.Should().Be("mqttPasswd");

      config.Logos.Length.Should().Be(1);
      var logo = config.Logos.First();

      logo.IpAddress.Should().Be("1.2.3.4");

      logo.MemoryRanges[0].LocalVariableMemoryStart.Should().Be(0);
      logo.MemoryRanges[0].LocalVariableMemoryEnd.Should().Be(128);
      logo.MemoryRanges[0].LocalVariableMemoryPollingCycleMilliseconds.Should().Be(10000);

      logo.MemoryRanges[1].LocalVariableMemoryStart.Should().Be(12);
      logo.MemoryRanges[1].LocalVariableMemoryEnd.Should().Be(42);
      logo.MemoryRanges[1].LocalVariableMemoryPollingCycleMilliseconds.Should().Be(666);

      logo.Mqtt[0].ClientId.Should().Be("mqtt-client-id");

      logo.Mqtt[0].Channels[0].Action.Should().Be("subscribeSet");
      logo.Mqtt[0].Channels[0].Topic.Should().Be("map/21/31/set");
      logo.Mqtt[0].Channels[0].LogoAddress.Should().Be(21);
      logo.Mqtt[0].Channels[0].Type.Should().Be("byte");

      logo.Mqtt[0].Channels[1].Action.Should().Be("subscribeSet");
      logo.Mqtt[0].Channels[1].Topic.Should().Be("map/22/32/set");
      logo.Mqtt[0].Channels[1].LogoAddress.Should().Be(22);
      logo.Mqtt[0].Channels[1].Type.Should().Be("integer");

      logo.Mqtt[0].Channels[2].Action.Should().Be("subscribeSet");
      logo.Mqtt[0].Channels[2].Topic.Should().Be("map/26/36/set");
      logo.Mqtt[0].Channels[2].LogoAddress.Should().Be(26);
      logo.Mqtt[0].Channels[2].Type.Should().Be("float");

      logo.Mqtt[0].Channels[3].Action.Should().Be("publish");
      logo.Mqtt[0].Channels[3].Topic.Should().Be("map/121/131/get");
      logo.Mqtt[0].Channels[3].LogoAddress.Should().Be(121);
      logo.Mqtt[0].Channels[3].Type.Should().Be("byte");

      logo.Mqtt[0].Channels[4].Action.Should().Be("publish");
      logo.Mqtt[0].Channels[4].Topic.Should().Be("map/122/132/get");
      logo.Mqtt[0].Channels[4].LogoAddress.Should().Be(122);
      logo.Mqtt[0].Channels[4].Type.Should().Be("integer");

      logo.Mqtt[0].Channels[5].Action.Should().Be("publish");
      logo.Mqtt[0].Channels[5].Topic.Should().Be("map/126/136/get");
      logo.Mqtt[0].Channels[5].LogoAddress.Should().Be(126);
      logo.Mqtt[0].Channels[5].Type.Should().Be("float");
    }
  }

  public class TempFile : IDisposable
  {
    public TempFile(string content)
    {
      Path = System.IO.Path.GetTempFileName();
      File.WriteAllText(Path, content);
    }

    public void Dispose() => File.Delete(Path);

    public string Path { get; }
  }
}