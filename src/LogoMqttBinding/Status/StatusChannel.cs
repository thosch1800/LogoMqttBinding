using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.MqttAdapter;
using MQTTnet;

namespace LogoMqttBinding.Status
{
  internal class StatusChannel : IAsyncDisposable
  {
    private readonly string identifier;

    public StatusChannel(string identifier)
    {
      this.identifier = identifier;
      scheduler = new Scheduler(SendUpdates);
    }

    public async ValueTask DisposeAsync() => await scheduler.DisposeAsync();



    public void Update(Connection connection) =>
      SendMessage(nameof(Connection), connection);

    public void Update(LastNotification lastNotification) =>
      SendMessage(nameof(LastNotification), lastNotification);



    public void Add(Mqtt mqttClient, MqttStatusChannelConfig? statusConfig)
    {
      if (statusConfig is null) return;

      var lastWill = BuildMessage(nameof(Connection), Connection.Interrupted, statusConfig);
      mqttClient.AddLastWill(lastWill);

      contexts.Add(new MqttContext(mqttClient, statusConfig));
    }



    private void SendMessage(string topic, string text)
    {
      ProvideDefaultChannels();
      scheduler.Queue(Identifier(topic), text);
    }


    private void ProvideDefaultChannels()
    {
      if (!isFirstMessage) return;
      isFirstMessage = false;

      var asm = typeof(StatusChannel).Assembly.GetName();
      var version = asm.Version?.ToString() ?? "";
      var software = $"{asm.Name ?? ""} {version}";

      SendMessage("Software", software);
      SendMessage("Version", version);
    }



    private string Identifier(string topic) => $"{identifier}/{topic}";

    private async Task SendUpdates()
    {
      foreach (var (topic, text) in scheduler.Messages())
      foreach (var (mqttClient, channelConfig) in contexts)
        await mqttClient
          .PublishAsync(BuildMessage(topic, text, channelConfig))
          .ConfigureAwait(false);
    }

    private static MqttApplicationMessage BuildMessage(string subStatusTopic, string message, MqttStatusChannelConfig config) => new MqttApplicationMessageBuilder()
      .WithPayload(message)
      .WithTopic(config.Topic + $"/{subStatusTopic}")
      .WithRetainFlag(config.Retain)
      .WithQualityOfServiceLevel(config.GetQualityOfServiceAsEnum().ToMqttNet())
      .Build();


    private bool isFirstMessage = true;
    private readonly Scheduler scheduler;
    private readonly List<MqttContext> contexts = new();

    internal record MqttContext(Mqtt Client, MqttStatusChannelConfig Config);
  }
}