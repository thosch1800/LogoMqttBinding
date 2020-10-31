using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LogoMqttBindingTests.Infrastructure;
using MQTTnet;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using Xunit;

namespace LogoMqttBindingTests
{
  [Collection(nameof(MqttLibraryTestEnvironment))]
  public class MqttLibraryTests
  {
    private readonly MqttLibraryTestEnvironment testEnvironment;
    public MqttLibraryTests(MqttLibraryTestEnvironment testEnvironment) => this.testEnvironment = testEnvironment;

    [Fact]
    public async Task Client1SubscribesTopic_Client2PublishesTopic_Client1Receives()
    {
      const string? topic = "this/is/the/current/topic";
      var payload = new byte[] { 42, 0, 8, 15, 47, 11 };

      MqttApplicationMessage? actuallyReceived = null;
      testEnvironment.MqttClient1!.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(
        e => actuallyReceived = e.ApplicationMessage);

      var sResult = await testEnvironment.MqttClient1!.SubscribeAsync(
        new MqttClientSubscribeOptionsBuilder()
          .WithTopicFilter(topic)
          .Build()
        , CancellationToken.None);

      var pResult = await testEnvironment.MqttClient2!.PublishAsync(
        new MqttApplicationMessageBuilder()
          .WithTopic(topic)
          .WithPayload(payload)
          .Build(),
        CancellationToken.None
      );

      await Task.Delay(100).ConfigureAwait(false); // let mqtt work

      sResult.Items.FirstOrDefault()?.ResultCode.Should().Be(MqttClientSubscribeResultCode.GrantedQoS0);
      pResult.ReasonCode.Should().Be(MqttClientPublishReasonCode.Success);
      actuallyReceived.Should().NotBeNull();
      actuallyReceived!.Payload.Should().Equal(payload);
    }

    [Fact]
    public async Task Client2SubscribesTopic_Client1PublishesTopic_Client1Receives()
    {
      const string? topic = "this/is/the/current/topic";
      var payload = new byte[] { 42, 0, 8, 15, 47, 11 };

      MqttApplicationMessage? actuallyReceived = null;
      testEnvironment.MqttClient2!.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(
        e => actuallyReceived = e.ApplicationMessage);

      var sResult = await testEnvironment.MqttClient2!.SubscribeAsync(
        new MqttClientSubscribeOptionsBuilder()
          .WithTopicFilter(topic)
          .Build()
        , CancellationToken.None);

      var pResult = await testEnvironment.MqttClient1!.PublishAsync(
        new MqttApplicationMessageBuilder()
          .WithTopic(topic)
          .WithPayload(payload)
          .Build(),
        CancellationToken.None
      );

      await Task.Delay(100).ConfigureAwait(false); // let mqtt work

      sResult.Items.FirstOrDefault()?.ResultCode.Should().Be(MqttClientSubscribeResultCode.GrantedQoS0);
      pResult.ReasonCode.Should().Be(MqttClientPublishReasonCode.Success);
      actuallyReceived.Should().NotBeNull();
      actuallyReceived!.Payload.Should().Equal(payload);
    }
  }
}