using System;

namespace LogoMqttBinding.LogoAdapter
{
  public interface ILogoVariable<T>
  {
    public T Get();

    public void Set(T value);

    public NotificationContext SubscribeToChangeNotification(Action<ILogoVariable<T>> onChanged);
  }
}