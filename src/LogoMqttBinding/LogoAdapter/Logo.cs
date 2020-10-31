using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using Microsoft.Extensions.Logging;
using Sharp7;

namespace LogoMqttBinding.LogoAdapter
{
  //TODO: how to pulse an PLC input (~250ms)?

  internal class Logo : IAsyncDisposable
  {
    public Logo(ILogger<Logo> logger, string ipAddress, params MemoryRangeConfig[] logoMemoryRanges)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      if (!IPAddress.TryParse(ipAddress, out _)) throw new ArgumentException("Invalid IP address.", nameof(ipAddress));
      this.ipAddress = ipAddress;

      this.logoMemoryRanges = ImmutableArray.Create(logoMemoryRanges
        .OrderBy(m => m.LocalVariableMemoryPollingCycleMilliseconds)
        .Select(memoryRange => new LogoMemory(this, memoryRange))
        .ToArray());

      Execute(c => c.SetConnectionParams(ipAddress, 0x200, 0x200));
    }

    public async ValueTask DisposeAsync()
    {
      SetUpdate(false);

      foreach (var memoryRange in logoMemoryRanges)
        await memoryRange
          .DisposeAsync()
          .ConfigureAwait(false);

      Execute(c => c.Disconnect());
    }

    public Int IntegerAt(int address) => new Int(this, address);
    public Float FloatAt(int address) => new Float(this, address);
    public Byte ByteAt(int address) => new Byte(this, address);


    public bool Connect()
    {
      lock (client)
      {
        if (client.Connected) return true;

        logger.LogMessage("connecting...", logLevel: LogLevel.Debug);
        Execute(c => c.Connect());
        logger.LogMessage($"connected:{client.Connected}", logLevel: LogLevel.Debug);
        SetUpdate(true);

        return client.Connected;
      }
    }

    public override string ToString() => $"{GetType().Name} {ipAddress}";



    /// <summary>
    ///   Executes the specified action on the logo client and checks if an error occurred. Logs error and returns true if an
    ///   error occurred, otherwise returns false.
    /// </summary>
    /// <param name="action">Logo client action</param>
    /// <param name="callerName">done by [CallerMemberName]</param>
    /// <param name="callerFile">done by [CallerFilePath]</param>
    /// <param name="callerFileLine">done by [CallerLineNumber]</param>
    /// <returns>Returns true if an error occurred, otherwise false</returns>
    internal bool Execute(
      Func<S7Client, int> action,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerFileLine = 0)
    {
      lock (clientLock)
      {
        var error = action(client);
        if (error == 0) return false;

        var message = $"{ToString()} Error {error}/0x{error:X8} {client.ErrorText(error)}";
        logger.LogMessage(
          message,
          logLevel: LogLevel.Error,
          callerName: callerName,
          // ReSharper disable once ExplicitCallerInfoArgument
          callerFile: callerFile,
          // ReSharper disable once ExplicitCallerInfoArgument
          callerFileLine: callerFileLine
        );

        if (error == 9) Connect(); //Error 9: Client not connected

        return true;
      }
    }

    internal byte[] GetBytes(int address, int length)
      => GetMemoryRange(address, length)
        .GetBytes(address, length);

    internal NotificationContext SubscribeToChangeNotification(NotificationContext n)
      => GetMemoryRange(n.Address, n.Length)
        .SubscribeToChangeNotification(n);

    internal void UnsubscribeFromChangeNotification(NotificationContext n)
      => GetMemoryRange(n.Address, n.Length)
        .UnsubscribeFromChangeNotification(n);

    private void SetUpdate(bool active)
    {
      foreach (var memoryRange in logoMemoryRanges)
        memoryRange.SetUpdate(active);
    }

    private LogoMemory GetMemoryRange(int address, int length)
    {
      string PopulatedMethodSignature() => $"{nameof(Logo)}({ipAddress}).{nameof(GetBytes)}({nameof(address)}:{address}, {nameof(length)}:{length})";

      if (length < 1 || length > 850) throw new ArgumentOutOfRangeException(nameof(length), @$"{PopulatedMethodSignature()}: invalid length");

      var m = logoMemoryRanges.FirstOrDefault(memory => memory.Start <= address && address <= memory.End);
      if (m == null) throw new ArgumentException(@$"{PopulatedMethodSignature()}: no local variable memory range defined");

      return m;
    }

    private readonly string ipAddress;
    private readonly ILogger<Logo> logger;
    private readonly ImmutableArray<LogoMemory> logoMemoryRanges;
    private readonly object clientLock = new object();
    private readonly S7Client client = new S7Client();
  }
}