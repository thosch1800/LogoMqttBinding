using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using MQTTnet;

namespace LogoMqttBinding.MqttAdapter
{
  internal class StatusChannel : IAsyncDisposable
  {
    public StatusChannel()
    {
      timer = new Timer(
        async s => await SendStatusUpdate(),
        null,
        TimeSpan.FromMilliseconds(-1),
        TimeSpan.FromMilliseconds(-1));
    }

    public async ValueTask DisposeAsync()
    {
      await timer.DisposeAsync();
    }

    public void AddMqtt(Mqtt mqttClient, MqttStatusChannelConfig? statusConfig)
    {
      if (statusConfig is null) return;

      mqttClient.AddLastWill(
        BuildMqttMessage(
          GetStatusMessageFrom(ConnectionState.Interrupted),
          statusConfig));

      contexts.Add(new MqttContext(mqttClient, statusConfig));
    }

    public void Update(
      ConnectionState? connectionState = null,
      DateTime? notificationTimestamp = null)
    {
      lock (synchronizationContext)
      { // update state as needed
        connection = connectionState ?? connection;
        lastNotification = notificationTimestamp ?? lastNotification;
        ActivateTimer();
      }
    }

    private void ActivateTimer() => timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));

    private async Task SendStatusUpdate()
    {
      var statusMessage = GetStatusMessageFrom(connection);
      foreach (var context in contexts)
        await UpdateStatus(context, statusMessage);
    }

    private string GetStatusMessageFrom(ConnectionState connectionState)
    {
      lock (synchronizationContext)
        return @$"
Connection: {GetConnectionText(connectionState)}
Last notification: {GetNotificationText(lastNotification)}
";
    }

    private static string GetConnectionText(ConnectionState state) =>
      state switch
      {
        ConnectionState.Connected => "connected",
        ConnectionState.Disconnected => "disconnected",
        ConnectionState.Interrupted => "lost",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
      };

    private static string GetNotificationText(DateTime time) => time.ToLongTimeString();

    private async Task UpdateStatus(MqttContext context, string statusMessage)
    {
      var mqttMessage = BuildMqttMessage(statusMessage, context.Config);
      await context.Client.PublishAsync(mqttMessage);
    }

    private static MqttApplicationMessage BuildMqttMessage(string message, MqttStatusChannelConfig config)
    {
      return new MqttApplicationMessageBuilder()
        .WithPayload(message)
        .WithTopic(config.Topic)
        .WithRetainFlag(config.Retain)
        .WithQualityOfServiceLevel(config.GetQualityOfServiceAsEnum().ToMqttNet())
        .Build();
    }

    private ConnectionState connection = ConnectionState.Disconnected;
    private DateTime lastNotification = DateTime.MinValue;
    private readonly List<MqttContext> contexts = new();
    private readonly Timer timer;
    private readonly object synchronizationContext = new();

    public enum ConnectionState { Connected, Disconnected, Interrupted };

    internal record MqttContext(Mqtt Client, MqttStatusChannelConfig Config);
  }
}