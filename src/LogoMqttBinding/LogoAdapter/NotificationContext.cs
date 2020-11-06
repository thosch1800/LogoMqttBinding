using System;

namespace LogoMqttBinding.LogoAdapter
{
  public class NotificationContext<T> : NotificationContext
  {
    public NotificationContext(int address, int length, ILogoVariable<T> logoVariable, Action<ILogoVariable<T>> onChanged)
      : base(address, length)
    {
      this.logoVariable = logoVariable;
      this.onChanged = onChanged;
    }

    internal override void NotifyChanged() => onChanged(logoVariable);

    private readonly ILogoVariable<T> logoVariable;
    private readonly Action<ILogoVariable<T>> onChanged;
  }

  public abstract class NotificationContext
  {
    protected NotificationContext(int address, int length)
    {
      Address = address;
      Length = length;
    }

    public int Address { get; }
    public int Length { get; }
    public Guid Id { get; } = Guid.NewGuid();

    internal abstract void NotifyChanged();
  }
}