using System;
using System.Net;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using Xunit;

// ReSharper disable MemberCanBePrivate.Global

namespace LogoMqttBinding.Tests.Infrastructure
{
  [CollectionDefinition(nameof(IntegrationTestEnvironment), DisableParallelization = true)]
  public class IntegrationTestEnvironment : ICollectionFixture<IntegrationTestEnvironment>, IAsyncLifetime
  {
    public async Task InitializeAsync()
    {
      Logger = new TestableLogger();
      LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(c => c.ConfigureTestableLogger(Logger));

      LogoHardwareMock = new LogoHardwareMock();

      var brokerIpAddress = IPAddress.Loopback;
      var brokerPort = 1889;

      var mqttFactory = new MqttFactory();
      mqttServer = mqttFactory.CreateMqttServer();
      MqttClient = mqttFactory.CreateMqttClient();
      MqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args => MqttMessageReceived?.Invoke(this, args));

      var mqttServerOptions = new MqttServerOptionsBuilder()
        .WithClientId(nameof(IntegrationTestEnvironment) + "Broker")
        .WithDefaultEndpointBoundIPAddress(brokerIpAddress)
        .WithDefaultEndpointPort(brokerPort)
        .Build();

      var mqttClientOptions = new MqttClientOptionsBuilder()
        .WithClientId(nameof(IntegrationTestEnvironment) + "Client")
        .WithTcpServer(brokerIpAddress.ToString(), brokerPort)
        .Build();

      await mqttServer
        .StartAsync(mqttServerOptions)
        .ConfigureAwait(false);

      await MqttClient
        .ConnectAsync(mqttClientOptions)
        .ConfigureAwait(false);

      var config = IntegrationTests.GetConfig(brokerIpAddress.ToString(), brokerPort);
      config.Validate();
      appContext = Logic
        .Initialize(LoggerFactory, config);
      await appContext
        .Connect()
        .ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
      if (appContext != null)
        await appContext
          .DisposeAsync()
          .ConfigureAwait(false);

      await MqttClient
        .DisconnectAsync()
        .ConfigureAwait(false);

      if (mqttServer != null)
        await mqttServer
          .StopAsync()
          .ConfigureAwait(false);

      LogoHardwareMock?.Dispose();
    }


    public LogoHardwareMock? LogoHardwareMock { get; private set; }
    internal IMqttClient? MqttClient { get; private set; }
    public ILoggerFactory? LoggerFactory { get; private set; }
    public TestableLogger? Logger { get; private set; }
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MqttMessageReceived;

    private IMqttServer? mqttServer;
    private ProgramContext? appContext;
  }
}