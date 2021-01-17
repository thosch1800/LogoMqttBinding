using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogoMqttBinding.Configuration;
using LogoMqttBinding.Status;

namespace LogoMqttBinding.LogoAdapter
{
  internal class LogoMemory : IAsyncDisposable
  {
    public LogoMemory(Logo logo, MemoryRangeConfig memoryRangeConfig)
    {
      if (memoryRangeConfig == null) throw new ArgumentNullException(nameof(memoryRangeConfig));
      this.logo = logo ?? throw new ArgumentNullException(nameof(logo));
      pollingCycleMilliseconds = memoryRangeConfig.LocalVariableMemoryPollingCycleMilliseconds;
      Start = memoryRangeConfig.LocalVariableMemoryStart;
      End = memoryRangeConfig.LocalVariableMemoryEnd;
      size = End - Start;
      image = new byte[size];
      imageOfLastCycle = new byte[size];
      pollingLogicTask = Task.Factory.StartNew(PollingLogic, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public async ValueTask DisposeAsync()
    {
      cts.Cancel();
      await pollingLogicTask.ConfigureAwait(false);
    }

    public void EnableUpdate(bool active) => update = active;

    public int Start { get; }
    public int End { get; }

    public byte[] GetBytes(int address, int length)
    {
      byte[] result = new byte[length];

      imageLock.EnterReadLock();
      Array.Copy(image, address - Start, result, 0, length);
      imageLock.ExitReadLock();

      return result;
    }

    public NotificationContext SubscribeToChangeNotification(NotificationContext notificationContext)
    {
      lock (notificationContextsLock)
        notificationContexts.Add(notificationContext);
      return notificationContext;
    }

    public void UnsubscribeFromChangeNotification(NotificationContext notificationContext)
    {
      lock (notificationContextsLock)
        notificationContexts.RemoveAll(n => n.Id == notificationContext.Id);
    }

    private async Task PollingLogic()
    {
      var readBuffer = new byte[size];

      for (; !cts.Token.IsCancellationRequested; await Task.Delay(pollingCycleMilliseconds, cts.Token).ConfigureAwait(false))
      {
        if (!update) continue; // do nothing during initialization phase

        var hasErrors = logo.Execute(c => c.DBRead(1, Start, size, readBuffer));
        if (hasErrors) await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
        else
        {
          imageLock.EnterReadLock();
          Array.Copy(image, 0, imageOfLastCycle, 0, size);
          imageLock.ExitReadLock();

          imageLock.EnterWriteLock();
          Array.Copy(readBuffer, 0, image, 0, size);
          imageLock.ExitWriteLock();

          NotifyChanged();
        }
      }
    }

    private void NotifyChanged()
    {
      var changed = new List<NotificationContext>();

      lock (notificationContextsLock)
        foreach (NotificationContext notificationContext in notificationContexts)
        {
          imageLock.EnterReadLock();

          for (var address = notificationContext.Address; address < notificationContext.Address + notificationContext.Length; address++)
            if (image[address - Start] != imageOfLastCycle[address - Start])
            {
              changed.Add(notificationContext);
              break;
            }

          imageLock.ExitReadLock();
        }

      foreach (var context in changed)
        context.NotifyChanged();
    }


    private readonly int size;
    private readonly Logo logo;
    private volatile bool update;
    private readonly byte[] image;
    private readonly byte[] imageOfLastCycle;
    private readonly ReaderWriterLockSlim imageLock = new();
    private readonly CancellationTokenSource cts = new();
    private readonly object notificationContextsLock = new();
    private readonly List<NotificationContext> notificationContexts = new();
    private readonly Task pollingLogicTask;
    private readonly int pollingCycleMilliseconds;
  }
}