using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;
using Microsoft.Extensions.Logging;

namespace LogoMqttBinding
{
  internal class ProgramContext : IAsyncDisposable
  {
    internal ProgramContext(ILogger<ProgramContext> logger, ImmutableArray<Logo> logos, ImmutableArray<Mqtt> mqttClients)
    {
      this.logger = logger;
      this.logos = logos;
      this.mqttClients = mqttClients;
    }

    public async ValueTask DisposeAsync()
    {
      logger.LogInformation("Disposing...");

      foreach (var logo in logos)
        await logo.DisposeAsync().ConfigureAwait(false);

      foreach (var mqttClient in mqttClients)
        await mqttClient.DisposeAsync().ConfigureAwait(false);

      logger.LogInformation("Disposed...");
    }

    internal async Task Connect()
    {
      foreach (var logo in logos)
      {
        logger.LogInformation($"Connecting to {logo}");
        logo.Connect();
      }

      foreach (var mqttClient in mqttClients)
      {
        logger.LogInformation($"Connecting to {mqttClient}");
        await mqttClient.TryConnectAsync().ConfigureAwait(false);
      }
    }

    private ImmutableArray<Logo> logos;
    private ImmutableArray<Mqtt> mqttClients;
    private readonly ILogger<ProgramContext> logger;
  }
}