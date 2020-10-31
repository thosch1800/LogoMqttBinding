using System;

namespace LogoMqttBinding.LogoAdapter
{
  public class NotificationContext
  {
    public NotificationContext(int address, int length, Action onChanged)
    {
      this.onChanged = onChanged;
      Address = address;
      Length = length;
    }

    public int Address { get; }
    public int Length { get; }
    public Guid Id { get; } = Guid.NewGuid();

    internal void NotifyChanged() => onChanged();

    private readonly Action onChanged;
  }
}