using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LogoMqttBinding.LogoAdapter;
using LogoMqttBinding.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogoMqttBinding.Tests
{
  [Collection(nameof(LogoTestsEnvironment))]
  public class LogoTests
  {
    private readonly LogoTestsEnvironment logoTestsEnvironment;
    private readonly LogoHardwareMock logoHardwareMock;
    private readonly Logo logo;

    public LogoTests(LogoTestsEnvironment logoTestsEnvironment)
    {
      this.logoTestsEnvironment = logoTestsEnvironment;
      logoHardwareMock = logoTestsEnvironment.LogoHardwareMock;
      logo = logoTestsEnvironment.Logo;
    }

    [Fact]
    public void Ctor_LoggerNull_ThrowsException()
    {
      var ex = Assert.Throws<ArgumentNullException>(()
        => new Logo(null!, "127.0.0.1"));
      ex.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Ctor_MemoryRangeNull_ThrowsException()
    {
      var ex = Assert.Throws<ArgumentException>(()
        => new Logo(NullLogger<Logo>.Instance, "a.b.c.d"));
      ex.ParamName.Should().Be("ipAddress");
    }


    [Fact]
    public async Task Connect_ReturnsTrue()
    {
      bool firstConnectAttempt;
      int firstClientsCount;
      bool secondConnectAttempt;
      int secondClientsCount;

      {
        await using var instance = logoTestsEnvironment.CreateLogo();

        firstConnectAttempt = instance.Connect();
        firstClientsCount = logoHardwareMock.ClientsCount;

        secondConnectAttempt = instance.Connect();
        secondClientsCount = logoHardwareMock.ClientsCount;
      }
      await Task.Delay(1).ConfigureAwait(false); // wait some time to let dispose close the connection

      firstConnectAttempt.Should().BeTrue();
      firstClientsCount.Should().Be(2);
      secondConnectAttempt.Should().BeTrue();
      secondClientsCount.Should().Be(2);
      logoHardwareMock.ClientsCount.Should().Be(1);
    }



    /*
    [Fact]
    public async Task Connect_ToUnreachableHost_Fails()
    {
      var testableLogger = new TestableLogger<Logo>();
      await using var instance = logoTestsEnvironment.CreateLogo(testableLogger, "192.168.255.42");

      var connected = instance.Connect();

      connected.Should().BeFalse();
      var lastMessage = testableLogger.Messages.LastOrDefault();
      lastMessage
        .Should().NotBeNullOrEmpty()
        .And.Contain("Connection Error")
        .And.Contain("192.168.255.42");
    }*/



    [Fact]
    public void GetBytes_InvalidAddress_Throws()
    {
      var ex = Assert.Throws<ArgumentException>(()
        => logo.GetBytes(-1, 1));
      ex.Message.Should().Contain("address:-1");
    }

    [Fact]
    public void GetBytes_InvalidLength_Throws()
    {
      var ex = Assert.Throws<ArgumentOutOfRangeException>(()
        => logo.GetBytes(0, 0));
      ex.ParamName.Should().Be("length");
      ex.Message.Should().Contain("length:0");
    }



    [Theory]
    [InlineData(-1)]
    public void ByteAt_AddressInvalid_Throws(int address)
    {
      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => logo.ByteAt(address));
      ex.Message.Should().Contain("positive number");
      ex.ParamName.Should().Be("address");
    }

    [Theory]
    [InlineData(4, byte.MinValue)]
    [InlineData(4, byte.MaxValue)]
    public async Task ByteAt_Get(int address, byte value)
    {
      logoHardwareMock.WriteByte(address, value);
      await Task
        .Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds)
        .ConfigureAwait(false);

      var byteValue = logo.ByteAt(address).Get();

      byteValue.Should().Be(value);
    }

    [Theory]
    [InlineData(5, byte.MinValue)]
    [InlineData(5, byte.MaxValue)]
    public void ByteAt_Set(int address, byte value)
    {
      logo.ByteAt(address).Set(value);

      var actual = logoHardwareMock.ReadByte(address);
      actual.Should().Be(value);
    }

    [Theory]
    [InlineData(101, 42, 187)]
    public async Task ByteAt_OnChange(int address, byte firstValueUpdate, byte secondValueUpdate)
    {
      var notifiedValues = new List<byte>();

      var notificationContext = logo
        .ByteAt(address)
        .SubscribeToChangeNotification(
          logoVariable => notifiedValues.Add(logoVariable.Get()));

      logoHardwareMock.WriteByte(address, firstValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logoHardwareMock.WriteByte(address, secondValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logo.UnsubscribeFromChangeNotification(notificationContext);

      notifiedValues.Count.Should().Be(2);
      notifiedValues[0].Should().Be(firstValueUpdate);
      notifiedValues[1].Should().Be(secondValueUpdate);
    }



    [Theory]
    [InlineData(-1)]
    public void IntegerAt_AddressInvalid_Throws(int address)
    {
      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => logo.IntegerAt(address));
      ex.Message.Should().Contain("positive number");
      ex.ParamName.Should().Be("address");
    }

    [Theory]
    [InlineData(14, short.MinValue)]
    [InlineData(14, short.MaxValue)]
    public async Task IntegerAt_Get(int address, short value)
    {
      logoHardwareMock.WriteInteger(address, value);
      await Task
        .Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds)
        .ConfigureAwait(false);

      var integerValue = logo.IntegerAt(address).Get();

      integerValue.Should().Be(value);
    }

    [Theory]
    [InlineData(18, short.MinValue)]
    [InlineData(18, short.MaxValue)]
    public void IntegerAt_Set(int address, short value)
    {
      logo.IntegerAt(address).Set(value);

      var integerValue = logoHardwareMock.ReadInteger(address);
      integerValue.Should().Be(value);
    }

    [Theory]
    [InlineData(102, short.MinValue, short.MaxValue)]
    public async Task IntegerAt_OnChange(int address, short firstValueUpdate, short secondValueUpdate)
    {
      var notifiedValues = new List<short>();

      var notificationContext = logo
        .IntegerAt(address)
        .SubscribeToChangeNotification(
          logoVariable => notifiedValues.Add(logoVariable.Get()));

      logoHardwareMock.WriteInteger(address, firstValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logoHardwareMock.WriteInteger(address, secondValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logo.UnsubscribeFromChangeNotification(notificationContext);

      notifiedValues.Count.Should().Be(2);
      notifiedValues[0].Should().Be(firstValueUpdate);
      notifiedValues[1].Should().Be(secondValueUpdate);
    }

    [Theory]
    [InlineData(-1)]
    public void FloatAt_AddressInvalid_Throws(int address)
    {
      var ex = Assert.Throws<ArgumentOutOfRangeException>(() => logo.FloatAt(address));
      ex.Message.Should().Contain("positive number");
      ex.ParamName.Should().Be("address");
    }

    [Theory]
    [InlineData(6, float.MinValue)]
    [InlineData(6, float.MaxValue)]
    [InlineData(6, float.Epsilon)]
    [InlineData(6, float.NegativeInfinity)]
    [InlineData(6, float.PositiveInfinity)]
    [InlineData(6, float.NaN)]
    public async Task FloatAt_Get(int address, float value)
    {
      logoHardwareMock.WriteFloat(address, value);
      await Task
        .Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds)
        .ConfigureAwait(false);

      var floatValue = logo.FloatAt(address).Get();

      floatValue.Should().Be(value);
    }

    [Theory]
    [InlineData(10, float.MinValue)]
    [InlineData(10, float.MaxValue)]
    [InlineData(10, float.Epsilon)]
    [InlineData(10, float.NegativeInfinity)]
    [InlineData(10, float.PositiveInfinity)]
    [InlineData(10, float.NaN)]
    public void FloatAt_Set(int address, float value)
    {
      logo.FloatAt(address).Set(value);

      var floatValue = logoHardwareMock.ReadFloat(address);
      floatValue.Should().Be(value);
    }

    [Theory]
    [InlineData(109, float.MinValue, float.MaxValue, float.NaN, 1.234f)]
    public async Task FloatAt_OnChange(int address, float firstValueUpdate, float secondValueUpdate, float thirdValueUpdate, float fourthValueUpdate)
    {
      var notifiedValues = new List<float>();

      var notificationContext = logo
        .FloatAt(address)
        .SubscribeToChangeNotification(
          logoVariable => notifiedValues.Add(logoVariable.Get()));

      logoHardwareMock.WriteFloat(address, firstValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logoHardwareMock.WriteFloat(address, secondValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logoHardwareMock.WriteFloat(address, thirdValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logoHardwareMock.WriteFloat(address, fourthValueUpdate);
      await Task.Delay(logoTestsEnvironment.TestUpdateDelayMilliseconds).ConfigureAwait(false); // let update happen

      logo.UnsubscribeFromChangeNotification(notificationContext);

      notifiedValues.Count.Should().Be(4);
      notifiedValues[0].Should().Be(firstValueUpdate);
      notifiedValues[1].Should().Be(secondValueUpdate);
      notifiedValues[2].Should().Be(thirdValueUpdate);
      notifiedValues[3].Should().Be(fourthValueUpdate);
    }
  }
}