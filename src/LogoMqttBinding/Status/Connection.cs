using System;

namespace LogoMqttBinding.Status
{
  public class Connection
  {
    public static readonly Connection Connected = new(nameof(Connected));
    public static readonly Connection Disconnected = new(nameof(Disconnected));
    public static readonly Connection Interrupted = new(nameof(Interrupted));

    public static implicit operator string(Connection instance)
    {
      if (ReferenceEquals(instance, Connected)) return "connected";
      if (ReferenceEquals(instance, Disconnected)) return "disconnected";
      if (ReferenceEquals(instance, Interrupted)) return "lost";
      throw new ArgumentOutOfRangeException(nameof(state), instance.state, "Invalid " + nameof(Connection));
    }

    private Connection(string state) => this.state = state;
    private readonly string state;
  }
}