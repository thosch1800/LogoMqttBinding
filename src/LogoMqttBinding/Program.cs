using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    public static async Task Main()
    {
      Console.WriteLine("Configuring logger...");
      var loggerFactory = ConfigureLogging();
      try
      {
        Console.WriteLine("Reading configuration...");
        var configuration = ReadConfiguration();

        Console.WriteLine("Initializing...");
        await using var appContext = await Initialize(loggerFactory, configuration).ConfigureAwait(false);

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

    private static Config ReadConfiguration()
    {
      var configuration = new Config();
      configuration.Read();
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

    internal static async Task<ProgramContext> Initialize(ILoggerFactory loggerFactory, Config config)
    {
      var logos = new List<Logo>();
      var mqttClients = new List<Mqtt>();

      foreach (var logoConfig in config.Logos)
      {
        Console.WriteLine($"Logo {logoConfig.IpAddress}");

        var logo = new Logo(
          loggerFactory.CreateLogger<Logo>(),
          logoConfig.IpAddress,
          logoConfig.MemoryRanges);
        logos.Add(logo);

        foreach (var mqttConfig in logoConfig.Mqtt)
        {
          Console.WriteLine($"MQTT {mqttConfig.ClientId} -> {config.MqttBrokerIpAddress}");

          var mqttClient = new Mqtt(loggerFactory.CreateLogger<Mqtt>(), mqttConfig.ClientId, config.MqttBrokerIpAddress, config.MqttBrokerPort);
          mqttClients.Add(mqttClient);

          foreach (var chConfig in mqttConfig.Subscribed)
            mqttClient.Subscribe(chConfig.Topic).AddMessageHandler(logo, chConfig.Type, chConfig.LogoAddress);

          foreach (var chConfig in mqttConfig.Published)
            LogoMqttMapping.LogoNotifyOnChange(chConfig.Type, mqttClient, chConfig.Topic, logo, chConfig.LogoAddress);
        }
      }

      var programContext = new ProgramContext(logos.ToImmutableArray(), mqttClients.ToImmutableArray());

      await Connect(programContext);

      return new ProgramContext(logos.ToImmutableArray(), mqttClients.ToImmutableArray());
    }

    private static async Task Connect(ProgramContext ctx)
    {
      foreach (var logo in ctx.Logos) logo.Connect();
      foreach (var mqttClient in ctx.MqttClients) await mqttClient.ConnectAsync();
    }

    private static async Task WaitForCtrlCAsync(CancellationToken ct)
    {
      using var lockObject = new SemaphoreSlim(1, 1);
      await lockObject.WaitAsync(ct); // take lock
      // ReSharper disable once AccessToDisposedClosure
      Console.CancelKeyPress += (_, _) => lockObject.Release(); // release lock on CTRL+C
      await lockObject.WaitAsync(ct); // wait until lock is released
    }
  }
}