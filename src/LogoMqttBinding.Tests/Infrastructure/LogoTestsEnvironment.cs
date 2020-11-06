using System.Net;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.LogoAdapter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

#pragma warning disable 8618

// ReSharper disable MemberCanBePrivate.Global

namespace LogoMqttBinding.Tests.Infrastructure
{
  [CollectionDefinition(nameof(LogoTestsEnvironment))]
  public class LogoTestsEnvironment : ICollectionFixture<LogoTestsEnvironment>, IAsyncLifetime
  {
    public Task InitializeAsync()
    {
      Logger = new TestableLogger();
      LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(c =>
      {
        c.AddConsole();
        c.ConfigureTestableLogger(Logger);
      });

      LogoHardwareMock = new LogoHardwareMock();
      Logo = CreateLogo();
      Logo.Connect();

      return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
      await Logo.DisposeAsync().ConfigureAwait(false);
      LogoHardwareMock.Dispose();
    }


    /// <summary>
    ///   The time test cases should wait to let the cache update
    /// </summary>
    public int TestUpdateDelayMilliseconds { get; } = 50;

    public int PollingCycleMilliseconds { get; } = 25;

    public LogoHardwareMock LogoHardwareMock { get; private set; }
    internal Logo Logo { get; private set; }
    public ILoggerFactory LoggerFactory { get; private set; }
    public TestableLogger Logger { get; private set; }

    internal Logo CreateLogo(ILogger<Logo>? logger = null, string? ipAddress = null)
      => new Logo(
        logger ?? NullLogger<Logo>.Instance,
        ipAddress ?? IPAddress.Loopback.ToString(),
        new MemoryRangeConfig
        {
          LocalVariableMemoryStart = 0,
          LocalVariableMemoryEnd = 256,
          LocalVariableMemoryPollingCycleMilliseconds = PollingCycleMilliseconds,
        },
        new MemoryRangeConfig
        {
          LocalVariableMemoryStart = 500,
          LocalVariableMemoryEnd = 850,
          LocalVariableMemoryPollingCycleMilliseconds = PollingCycleMilliseconds,
        }
      );
  }
}