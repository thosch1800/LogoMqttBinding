using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.Status;
using Microsoft.Extensions.Logging;
using Sharp7;

// ReSharper disable ExplicitCallerInfoArgument

namespace LogoMqttBinding.LogoAdapter
{
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

      StatusChannel = new StatusChannel(ipAddress);

      Execute(c => c.SetConnectionParams(ipAddress, 0x200, 0x200));
    }

    public async ValueTask DisposeAsync()
    {
      EnableUpdates(false);

      foreach (var memoryRange in logoMemoryRanges)
        await memoryRange
          .DisposeAsync()
          .ConfigureAwait(false);

      Disconnect();

      await StatusChannel.DisposeAsync();
    }

    public StatusChannel StatusChannel { get; }

    public Int IntegerAt(int address) => new(this, address);
    public Float FloatAt(int address) => new(this, address);
    public Byte ByteAt(int address) => new(this, address);


    public bool Connect()
    {
      var connected = false;
      lock (clientLock)
      {
        if (client.Connected) return true;

        logger.LogMessage("connecting...", logLevel: LogLevel.Debug);
        client.Connect();
        connected = client.Connected;
        logger.LogMessage($"connected:{connected}", logLevel: LogLevel.Debug);
        EnableUpdates(true);
      }

      if (connected)
        StatusChannel.Update(Connection.Connected);

      return connected;
    }

    private void Disconnect()
    {
      var wasConnected = false;
      lock (clientLock)
      {
        wasConnected = client.Connected;
        logger.LogMessage("disconnecting...", logLevel: LogLevel.Debug);
        client.Disconnect();
        logger.LogMessage($"disconnected", logLevel: LogLevel.Debug);
      }

      if (wasConnected)
        StatusChannel.Update(Connection.Disconnected);
    }


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

        var message = $"{GetType().Name} {ipAddress} Error {error}/0x{error:X8} {client.ErrorText(error)}";
        logger.LogMessage(message, logLevel: LogLevel.Error, callerName: callerName, callerFile: callerFile, callerFileLine: callerFileLine);

        HandleError();

        return true;
      }
    }

    private void HandleError()
    { // try to solve error by reconnect...
      Disconnect();
      Connect();
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

    private void EnableUpdates(bool active)
    {
      foreach (var memoryRange in logoMemoryRanges)
        memoryRange.EnableUpdate(active);
    }

    private LogoMemory GetMemoryRange(int address, int length)
    {
      string PopulatedMethodSignature() => $"{nameof(Logo)}({ipAddress}).{nameof(GetBytes)}({nameof(address)}:{address}, {nameof(length)}:{length})";

      if (length < 1 || length > 850) throw new ArgumentOutOfRangeException(nameof(length), @$"{PopulatedMethodSignature()}: invalid length");

      var m = logoMemoryRanges.FirstOrDefault(memory => memory.Start <= address && address <= memory.End);
      if (m == null) throw new ArgumentException(@$"{PopulatedMethodSignature()}: no local variable memory range defined");

      return m;
    }

    private readonly object clientLock = new();
    private readonly S7Client client = new();
    private readonly string ipAddress;
    private readonly ILogger<Logo> logger;
    private readonly ImmutableArray<LogoMemory> logoMemoryRanges;
  }
}