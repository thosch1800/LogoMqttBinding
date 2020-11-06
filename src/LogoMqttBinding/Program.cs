using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;

namespace LogoMqttBinding
{
  public static class Program
  {
    private const string ConfigPath = "./config/logo-mqtt.json";
    private const string DefaultConfigPath = "./configDefaults/logo-mqtt.json";

    public static async Task Main()
    {
      Console.WriteLine("Configuring logger...");
      var loggerFactory = ConfigureLogging();
      try
      {
        EnsureDefaultConfig();

        Console.WriteLine($"Reading configuration from {ConfigPath}...");
        var configuration = ReadConfiguration();

        Console.WriteLine("Initializing...");
        await using var appContext = Initialize(loggerFactory, configuration);

        Console.WriteLine("Connecting...");
        await Connect(appContext);

        Console.WriteLine("Press CTRL+C to exit");
        await WaitForCtrlCAsync(CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine("Exiting...");
      }
      catch (Exception ex)
      {
        loggerFactory
          .CreateLogger(nameof(Program))
          .LogException(ex);
        throw;
      }
    }

    internal static ProgramContext Initialize(ILoggerFactory loggerFactory, Config config)
    {
      var logos = new List<Logo>();
      var mqttClients = new List<Mqtt>();

      Console.WriteLine($"MQTT broker {config.MqttBrokerUri}:{config.MqttBrokerPort}");

      foreach (var logoConfig in config.Logos)
      {
        Console.WriteLine($"Logo at {logoConfig.IpAddress}");

        var logo = new Logo(
          loggerFactory.CreateLogger<Logo>(),
          logoConfig.IpAddress,
          logoConfig.MemoryRanges);
        logos.Add(logo);

        foreach (var mqttConfig in logoConfig.Mqtt)
        {
          Console.WriteLine($"| MQTT client {mqttConfig.ClientId}");

          var mqttClient = new Mqtt(
            loggerFactory.CreateLogger<Mqtt>(),
            mqttConfig.ClientId,
            config.MqttBrokerUri,
            config.MqttBrokerPort,
            config.MqttBrokerUsername,
            config.MqttBrokerPassword);
          mqttClients.Add(mqttClient);

          foreach (var chConfig in mqttConfig.Subscribed)
          {
            Console.WriteLine($"| | subscribe {chConfig.Topic} {chConfig.LogoAddress}/{chConfig.Type}");
            mqttClient.Subscribe(chConfig.Topic).AddMessageHandler(logo, chConfig.Type, chConfig.LogoAddress);
          }

          foreach (var chConfig in mqttConfig.Published)
          {
            Console.WriteLine($"| | publish {chConfig.Topic} {chConfig.LogoAddress}/{chConfig.Type}");
            LogoMqttMapping.LogoNotifyOnChange(chConfig.Type, mqttClient, chConfig.Topic, logo, chConfig.LogoAddress);
          }
        }
      }

      return new ProgramContext(logos.ToImmutableArray(), mqttClients.ToImmutableArray());
    }

    private static void EnsureDefaultConfig()
    {
      if (!File.Exists(ConfigPath))
      {
        Console.WriteLine($"Copy default configuration to {ConfigPath}...");
        File.Copy(DefaultConfigPath, ConfigPath);
      }
    }

    private static Config ReadConfiguration()
    {
      var configuration = new Config();
      configuration.Read(ConfigPath);
      configuration.Validate();
      return configuration;
    }

    private static ILoggerFactory ConfigureLogging()
    {
      return LoggerFactory.Create(
        c =>
        {
          c.AddConsole();
#if DEBUG
          c.SetMinimumLevel(LogLevel.Debug);
#endif
        });
    }

    internal static async Task Connect(ProgramContext ctx)
    {
      foreach (var logo in ctx.Logos) logo.Connect();
      foreach (var mqttClient in ctx.MqttClients) await mqttClient.ConnectAsync();
    }

    private static async Task WaitForCtrlCAsync(CancellationToken ct)
    {
      using var lockObject = new SemaphoreSlim(1, 1);
      await lockObject.WaitAsync(ct);
      Console.CancelKeyPress += (_, e) =>
      { // ReSharper disable once AccessToDisposedClosure
        lockObject.Release();
        e.Cancel = true;
      };
      await lockObject.WaitAsync(ct);
    }
  }
}