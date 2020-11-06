using System;
using FluentAssertions;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogoMqttBinding.Tests
{
  public class LogoMemoryTests
  {
    [Fact]
    public void Ctor_LogoNull_ThrowsException()
    {
      var ex = Assert.Throws<ArgumentNullException>(()
        => new LogoMemory(null!, new MemoryRangeConfig()));
      ex.ParamName.Should().Be("logo");
    }

    [Fact]
    public void Ctor_MemoryRangeNull_ThrowsException()
    {
      var ex = Assert.Throws<ArgumentNullException>(()
        => new LogoMemory(new Logo(NullLogger<Logo>.Instance, "127.0.0.1"), null!));
      ex.ParamName.Should().Be("memoryRangeConfig");
    }
  }
}