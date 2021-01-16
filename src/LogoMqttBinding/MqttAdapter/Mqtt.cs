using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;

namespace LogoMqttBinding.MqttAdapter
{
  public class Mqtt : IAsyncDisposable
  {
    public Mqtt(
      ILogger<Mqtt> logger,
      string clientId,
      string serverUri,
      int port,
      bool cleanSession,
      string? brokerUsername,
      string? brokerPassword,
      // ReSharper disable once SuggestBaseTypeForParameter
      MqttStatusChannelConfig? status)
    {
      this.logger = logger;
      this.serverUri = serverUri;
      this.clientId = clientId;

      var factory = new MqttFactory();
      client = factory.CreateMqttClient();
      client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(DisconnectedHandler);
      client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(ConnectedHandler);
      client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(MessageReceivedHandler);

      var clientOptionsBuilder = new MqttClientOptionsBuilder()
        .WithTcpServer(serverUri, port)
        .WithClientId(clientId)
        .WithCleanSession(cleanSession);

      if (status is not null)
        clientOptionsBuilder.WithWillMessage(
          new MqttApplicationMessageBuilder()
            .WithPayload("connection lost")
            .WithTopic(status.Topic)
            .WithRetainFlag(status.Retain)
            .WithQualityOfServiceLevel(status.GetQualityOfServiceAsEnum().ToMqttNet())
            .Build());
      
      if (brokerUsername is not null &&
          brokerPassword is not null)
        clientOptionsBuilder.WithCredentials(brokerUsername, brokerPassword);

      clientOptions = clientOptionsBuilder.Build();
    }

    public async ValueTask DisposeAsync()
    {
      isDisposing = true;

      if (client.IsConnected)
        await DisconnectAsync()
          .ConfigureAwait(false);

      client.Dispose();
    }



    public async Task TryConnectAsync()
    {
      try { await ConnectAsync(); }
      catch { logger.LogWarning($"connection failed {this}"); }
    }

    public async Task ConnectAsync()
    {
      await client
        .ConnectAsync(clientOptions)
        .ConfigureAwait(false);

      foreach (var subscription in subscriptions.Values)
      {
        var result = await client
          .SubscribeAsync(subscription.Topic, subscription.Qos)
          .ConfigureAwait(false);

        HandleSubscriptionResult(result, subscription);
      }
    }

    public async Task PublishAsync(MqttApplicationMessage message)
    {
      if (!client.IsConnected)
      {
        logger.LogMessage("currently not connected - cannot publish message",
          logLevel: LogLevel.Warning,
          args: a => a
            .Add(nameof(serverUri), serverUri)
            .Add(nameof(clientId), client));
        return;
      }

      try
      {
        await client
          .PublishAsync(message)
          .ConfigureAwait(false);
      }
      catch (Exception ex) { logger.LogException(ex); }
    }


    public Subscription Subscribe(string topic, MqttQualityOfServiceLevel qualityOfService)
    {
      var subscription = new Subscription(topic, qualityOfService);
      subscriptions.Add(topic, subscription);
      return subscription;
    }


    public override string ToString() => $"MQTT client {clientId} -> {serverUri}";



    private void HandleSubscriptionResult(MqttClientSubscribeResult subscribeResult, Subscription subscription)
    {
      var resultItem = subscribeResult.Items.First();
      switch (subscription.Qos)
      {
        case MqttQualityOfServiceLevel.AtMostOnce:
          LogOnError(resultItem.ResultCode, MqttClientSubscribeResultCode.GrantedQoS0);
          break;

        case MqttQualityOfServiceLevel.AtLeastOnce:
          LogOnError(resultItem.ResultCode, MqttClientSubscribeResultCode.GrantedQoS1);
          break;

        case MqttQualityOfServiceLevel.ExactlyOnce:
          LogOnError(resultItem.ResultCode, MqttClientSubscribeResultCode.GrantedQoS2);
          break;

        default: throw new ArgumentOutOfRangeException();
      }

      void LogOnError(MqttClientSubscribeResultCode actual, MqttClientSubscribeResultCode expected)
      {
        if (actual != expected)
          logger.LogMessage($"received result code {actual}, but expected {expected}",
            a => a
              .Add(nameof(subscription.Topic), subscription.Topic)
              .Add(nameof(subscription.Qos), subscription.Qos),
            LogLevel.Error);
      }
    }

    private void MessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
    {
      if (subscriptions.TryGetValue(e.ApplicationMessage.Topic, out var subscription))
        subscription.OnMessageReceived(e);
    }

    private async Task ConnectedHandler(MqttClientConnectedEventArgs e)
    {
      foreach (var subscription in subscriptions.Values)
        await client.SubscribeAsync(subscription.Topic).ConfigureAwait(false);

      await Task.CompletedTask;
    }

    private async Task DisconnectedHandler(MqttClientDisconnectedEventArgs e)
    {
      logger.LogInformation($"{clientId} disconnected from server {serverUri}");
      if (isDisposing) return;

      await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

      logger.LogDebug($"{clientId} reconnecting to {serverUri}");
      try
      {
        await client.ReconnectAsync().ConfigureAwait(false);
        logger.LogDebug($"{clientId} reconnected to {serverUri}");
      }
      catch { logger.LogWarning($"{clientId} reconnecting to server {serverUri} failed"); }
    }

    private async Task DisconnectAsync()
    {
      foreach (var subscription in subscriptions.Values)
        await client.UnsubscribeAsync(subscription.Topic);

      await client.DisconnectAsync().ConfigureAwait(false);
    }

    private bool isDisposing;
    private readonly string clientId;
    private readonly string serverUri;
    private readonly IMqttClient client;
    private readonly ILogger<Mqtt> logger;
    private readonly IMqttClientOptions clientOptions;
    private readonly Dictionary<string, Subscription> subscriptions = new();

    public class Subscription
    {
      internal Subscription(string topic, MqttQualityOfServiceLevel qos)
      {
        Topic = topic;
        Qos = qos;
      }

      public string Topic { get; }
      public MqttQualityOfServiceLevel Qos { get; }

      internal void OnMessageReceived(MqttApplicationMessageReceivedEventArgs e) => MessageReceived?.Invoke(this, e);
      public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;
    }
  }
}