using System;
using Sharp7;

namespace LogoMqttBinding.LogoAdapter
{
  internal class Float
  {
    public Float(Logo logo, int address)
    {
      if (address < 0) throw new ArgumentOutOfRangeException(nameof(address), "must be a positive number");
      this.logo = logo ?? throw new ArgumentNullException(nameof(logo));
      this.address = address;
    }

    public float Get()
    {
      var cached = logo.GetBytes(address, sizeof(float));
      var result = cached.GetRealAt(0);
      return result;
    }

    public void Set(float value)
    {
      var buffer = new byte[sizeof(float)];
      buffer.SetRealAt(0, value);
      logo.Execute(c => c.DBWrite(1, address, sizeof(float), buffer));
    }

    public NotificationContext SubscribeToChangeNotification(Action onChanged)
      => logo.SubscribeToChangeNotification(
        new NotificationContext(address, sizeof(float), onChanged));

    private readonly Logo logo;
    private readonly int address;
  }
}