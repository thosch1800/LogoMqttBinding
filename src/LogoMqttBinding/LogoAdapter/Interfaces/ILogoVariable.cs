using System;

namespace LogoMqttBinding.LogoAdapter.Interfaces
{
  public interface ILogoVariable<T>
  {
    public T Get();

    public void Set(T value);

    // ReSharper disable once UnusedMemberInSuper.Global
    public NotificationContext SubscribeToChangeNotification(Action<ILogoVariable<T>> onChanged);
  }
}