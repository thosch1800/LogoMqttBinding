using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogoMqttBinding.Status
{
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



    public void Queue(string topic, string text)
    {
      messageQueue.Enqueue(new Message(topic, text));
      timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));
    }

    public IEnumerable<Message> Messages()
    {
      while (messageQueue.TryDequeue(out var message))
        yield return message;
    }



    private readonly Timer timer;
    private readonly ConcurrentQueue<Message> messageQueue = new();

    public record Message(string Topic, string Text);
  }
}