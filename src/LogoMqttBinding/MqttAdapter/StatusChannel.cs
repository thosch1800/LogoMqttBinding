using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using MQTTnet;
using MQTTnet.Internal;

namespace LogoMqttBinding.MqttAdapter
{
  internal class StatusChannel : IAsyncDisposable
  {
    public StatusChannel()
    {
      timer = new Timer(
        async s => await SendUpdates(),
        null,
        TimeSpan.FromMilliseconds(-1),
        TimeSpan.FromMilliseconds(-1));
    }

    public async ValueTask DisposeAsync() => await timer.DisposeAsync();


    public void AddMqtt(Mqtt mqttClient, MqttStatusChannelConfig? statusConfig)
    {
      if (statusConfig is null) return;

      mqttClient.AddLastWill(
        BuildMessage(
          GetConnectionText(ConnectionState.Interrupted),
          statusConfig,
          Status.Connection));

      contexts.Add(new MqttContext(mqttClient, statusConfig));
    }


    public enum ConnectionState { Connected, Disconnected, Interrupted };

    public void UpdateConnection(ConnectionState connectionState)
    {
      messageQueue.Enqueue((Status.Connection, GetConnectionText(connectionState)));
      ScheduleSend();
    }

    public void UpdateNotificationTime()
    {
      messageQueue.Enqueue((Status.Notification, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)));
      ScheduleSend();
    }


    private void ScheduleSend() => timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));

    private async Task SendUpdates()
    {
      var (topic, message) = messageQueue.Dequeue();
      foreach (var context in contexts)
        await Send(context, message, topic);
    }

    private static string GetConnectionText(ConnectionState state) =>
      state switch
      {
        ConnectionState.Connected => "connected",
        ConnectionState.Disconnected => "disconnected",
        ConnectionState.Interrupted => "lost",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
      };

    private async Task Send(MqttContext context, string statusMessage, string subStatusTopic)
    {
      var mqttMessage = BuildMessage(statusMessage, context.Config, subStatusTopic);
      await context.Client.PublishAsync(mqttMessage);
    }

    private static MqttApplicationMessage BuildMessage(string message, MqttStatusChannelConfig config, string subStatusTopic)
    {
      return new MqttApplicationMessageBuilder()
        .WithPayload(message)
        .WithTopic(config.Topic + $"/{subStatusTopic}")
        .WithRetainFlag(config.Retain)
        .WithQualityOfServiceLevel(config.GetQualityOfServiceAsEnum().ToMqttNet())
        .Build();
    }

    private readonly BlockingQueue<(string, string)> messageQueue = new();
    private readonly List<MqttContext> contexts = new();
    private readonly Timer timer;

    internal record MqttContext(Mqtt Client, MqttStatusChannelConfig Config);
  }

  static class Status
  {
    public static string Connection => nameof(Connection);
    public static string Notification => nameof(Notification);
  }
}