using System;
using System.Net;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using Xunit;

// ReSharper disable MemberCanBePrivate.Global

namespace LogoMqttBindingTests.Infrastructure
{
  [CollectionDefinition(nameof(MqttLibraryTestEnvironment), DisableParallelization = true)]
  public class MqttLibraryTestEnvironment : ICollectionFixture<MqttLibraryTestEnvironment>, IAsyncLifetime
  {
    public async Task InitializeAsync()
    {
      var factory = new MqttFactory();
      MqttServer = factory.CreateMqttServer();
      MqttClient1 = factory.CreateMqttClient();
      MqttClient2 = factory.CreateMqttClient();
      MqttClient1.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args => Client1MessageReceived?.Invoke(this, args));
      MqttClient2.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args => Client2MessageReceived?.Invoke(this, args));

      var brokerIpAddress = IPAddress.Loopback;
      var brokerPort = 1885;

      var mqttServerOptions = new MqttServerOptionsBuilder()
        .WithClientId("broker")
        .WithDefaultEndpointBoundIPAddress(brokerIpAddress)
        .WithDefaultEndpointPort(brokerPort)
        .Build();
      var mqttClient1Options = new MqttClientOptionsBuilder()
        .WithClientId("client1")
        .WithTcpServer(brokerIpAddress.ToString(), brokerPort)
        .Build();
      var mqttClient2Options = new MqttClientOptionsBuilder()
        .WithClientId("client2")
        .WithTcpServer(brokerIpAddress.ToString(), brokerPort)
        .Build();

      await MqttServer.StartAsync(mqttServerOptions).ConfigureAwait(false);
      var connectClient1Result = await MqttClient1.ConnectAsync(mqttClient1Options).ConfigureAwait(false);
      var connectClient2Result = await MqttClient2.ConnectAsync(mqttClient2Options).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
      if (MqttClient1 != null) await MqttClient1.DisconnectAsync().ConfigureAwait(false);
      if (MqttClient2 != null) await MqttClient2.DisconnectAsync().ConfigureAwait(false);
      if (MqttServer != null) await MqttServer.StopAsync().ConfigureAwait(false);
    }

    internal IMqttClient? MqttClient1 { get; private set; }
    internal IMqttClient? MqttClient2 { get; private set; }
    internal IMqttServer? MqttServer { get; private set; }

    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? Client1MessageReceived;
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? Client2MessageReceived;
  }
}