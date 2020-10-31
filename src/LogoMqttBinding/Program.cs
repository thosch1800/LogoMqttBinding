using System;
using System.Collections.Generic;
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
      await using var appContext =
        await Initialize(
            ConfigureLogging(),
            ReadConfiguration())
          .ConfigureAwait(false);
      await WaitForCtrlCAsync(CancellationToken.None)
        .ConfigureAwait(false);
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

    internal static async Task<AppContext> Initialize(ILoggerFactory loggerFactory, Config config)
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

      foreach (var logo in logos)
      {
        logo.Connect();
        foreach (var mqttClient in mqttClients)
          await mqttClient.ConnectAsync();
      }

      return new AppContext(logos, mqttClients);
    }

    public class AppContext : IAsyncDisposable
    {
      internal AppContext(IEnumerable<Logo> logos, IEnumerable<Mqtt> mqttClients)
      {
        this.logos = logos;
        this.mqttClients = mqttClients;
      }

      public async ValueTask DisposeAsync()
      {
        // ReSharper disable once HeapView.ObjectAllocation.Possible
        foreach (var logo in logos) await logo.DisposeAsync();

        // ReSharper disable once HeapView.ObjectAllocation.Possible
        foreach (var mqttClient in mqttClients) await mqttClient.DisposeAsync();
      }

      private readonly IEnumerable<Logo> logos;
      private readonly IEnumerable<Mqtt> mqttClients;
    }

    private static async Task WaitForCtrlCAsync(CancellationToken ct)
    {
      using var lockObject = new SemaphoreSlim(1, 1);
      await lockObject.WaitAsync(ct); // take lock

      // ReSharper disable once AccessToDisposedClosure
      Console.CancelKeyPress += (s, e) => lockObject.Release(); // release lock on CTRL+C
      Console.WriteLine("Press CTRL+C to exit");

      await lockObject.WaitAsync(ct); // wait until lock is released
      Console.WriteLine("Exiting...");
    }
  }
}