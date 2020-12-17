using System;
using LogoMqttBinding.LogoAdapter.Interfaces;

namespace LogoMqttBinding.LogoAdapter
{
  public record NotificationContext<T> : NotificationContext
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

  public abstract record NotificationContext(int Address, int Length)
  {
    public Guid Id { get; } = Guid.NewGuid();

    internal abstract void NotifyChanged();
  }
}