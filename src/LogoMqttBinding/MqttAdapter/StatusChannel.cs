using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using MQTTnet;

namespace LogoMqttBinding.MqttAdapter
{
  internal class StatusChannel : IAsyncDisposable
  {
    public StatusChannel() => scheduler = new Scheduler(SendUpdates);
    public async ValueTask DisposeAsync() => await scheduler.DisposeAsync();



    public void Add(Mqtt mqttClient, MqttStatusChannelConfig? statusConfig)
    {
      if (statusConfig is null) return;

      mqttClient.AddLastWill(
        BuildMessage(
          nameof(Connection),
          Connection.Interrupted,
          statusConfig));

      contexts.Add(new MqttContext(mqttClient, statusConfig));
    }



    public void Update(Connection connection)
    {
      messageQueue.Enqueue((nameof(Connection), connection));
      scheduler.Send();
    }

    public void UpdateLastNotified()
    {
      messageQueue.Enqueue(("nameof(Notification)", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)));
      scheduler.Send();
    }



    private async Task SendUpdates()
    {
      if (messageQueue.TryDequeue(out var queued))
      {
        var (topic, message) = queued;
        foreach (var (mqttClient, channelConfig) in contexts)
        {
          var mqttMessage = BuildMessage(topic, message, channelConfig);
          await mqttClient.PublishAsync(mqttMessage);
        }
      }
    }

    private static MqttApplicationMessage BuildMessage(string subStatusTopic, string message, MqttStatusChannelConfig config) => new MqttApplicationMessageBuilder()
      .WithPayload(message)
      .WithTopic(config.Topic + $"/{subStatusTopic}")
      .WithRetainFlag(config.Retain)
      .WithQualityOfServiceLevel(config.GetQualityOfServiceAsEnum().ToMqttNet())
      .Build();

    private readonly ConcurrentQueue<(string, string)> messageQueue = new();
    private readonly List<MqttContext> contexts = new();
    private readonly Scheduler scheduler;

    internal record MqttContext(Mqtt Client, MqttStatusChannelConfig Config);
  }

  public class Scheduler : IAsyncDisposable
  {
    public Scheduler(Func<Task> callback)
    {
      timer = new Timer(
        async s => await callback(),
        null,
        TimeSpan.FromMilliseconds(-1),
        TimeSpan.FromMilliseconds(-1));
    }

    public async ValueTask DisposeAsync() => await timer.DisposeAsync();
    public void Send() => timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));

    private readonly Timer timer;
  }

  public class Connection
  {
    public static readonly Connection Connected = new(nameof(Connected));
    public static readonly Connection Disconnected = new(nameof(Disconnected));
    public static readonly Connection Interrupted = new(nameof(Interrupted));

    public static implicit operator string(Connection instance)
    {
      if (ReferenceEquals(instance, Connected)) return "connected";
      if (ReferenceEquals(instance, Disconnected)) return "disconnected";
      if (ReferenceEquals(instance, Interrupted)) return "lost";
      throw new ArgumentOutOfRangeException(nameof(state), instance.state, "Invalid " + nameof(Connection));
    }

    private Connection(string state) => this.state = state;
    private readonly string state;
  }

  // Notification
  //DateTime.UtcNow.ToString(CultureInfo.InvariantCulture))
}