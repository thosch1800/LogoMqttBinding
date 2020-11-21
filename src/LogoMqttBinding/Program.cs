using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using Microsoft.Extensions.Logging;

namespace LogoMqttBinding
{
  public class Program
  {
    public static async Task Main() => await new Program().Run();

    private Program()
    {
      loggerFactory = LoggerFactory.Create(c =>
      {
        c.AddConsole();
#if DEBUG
        c.SetMinimumLevel(LogLevel.Debug);
#endif
      });
      logger = loggerFactory.CreateLogger<Program>();
    }

    private async Task Run()
    {
      try
      {
        logger.LogInformation("Configuring...");
        await using var context = Configure();

        logger.LogInformation("Connecting...");
        await context.Connect().ConfigureAwait(false);

        logger.LogInformation("Up and running, press CTRL+C to exit...");
        await WaitForCtrlCAsync(CancellationToken.None).ConfigureAwait(false);

        logger.LogInformation("Closing...");
      }
      catch (Exception ex)
      {
        logger.LogException(ex);
        throw;
      }
    }

    private ProgramContext Configure()
    {
      var configuration = new Config();

      if (!File.Exists(ConfigPath))
      {
        logger.LogInformation("Creating default configuration...");
        File.Copy(DefaultConfigPath, ConfigPath);
      }

      logger.LogInformation($"Reading configuration from {ConfigPath}...");
      configuration.Read(ConfigPath);

      logger.LogInformation("Validating configuration...");
      configuration.Validate();

      logger.LogInformation("Initializing...");
      return Logic.Initialize(loggerFactory, configuration);
    }

    private static async Task WaitForCtrlCAsync(CancellationToken ct)
    {
      using var semaphore = new SemaphoreSlim(1, 1);
      await semaphore.WaitAsync(ct).ConfigureAwait(false);
      Console.CancelKeyPress += (_, e) =>
      { // ReSharper disable once AccessToDisposedClosure
        semaphore.Release();
        e.Cancel = true;
      };
      await semaphore.WaitAsync(ct).ConfigureAwait(false);
    }

    private readonly ILogger<Program> logger;
    private readonly ILoggerFactory loggerFactory;
    private const string ConfigPath = "./config/logo-mqtt.json";
    private const string DefaultConfigPath = "./configDefaults/logo-mqtt.json";
  }
}