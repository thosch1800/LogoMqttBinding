using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.MqttAdapter;

namespace LogoMqttBinding
{
  internal class AppContext : IAsyncDisposable
  {
    internal AppContext(ImmutableArray<Logo> logos, ImmutableArray<Mqtt> mqttClients)
    {
      Logos = logos;
      MqttClients = mqttClients;
    }

    public ImmutableArray<Logo> Logos { get; }
    public ImmutableArray<Mqtt> MqttClients { get; }

    public async ValueTask DisposeAsync()
    {
      foreach (var logo in Logos) await logo.DisposeAsync();
      foreach (var mqttClient in MqttClients) await mqttClient.DisposeAsync();
    }
  }
}