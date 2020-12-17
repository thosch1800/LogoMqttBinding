using System;
using LogoMqttBinding.LogoAdapter.Interfaces;
using Sharp7;

namespace LogoMqttBinding.LogoAdapter
{
  internal class Float : ILogoVariable<float>
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

    public NotificationContext SubscribeToChangeNotification(Action<ILogoVariable<float>> onChanged)
      => logo.SubscribeToChangeNotification(
        new NotificationContext<float>(address, sizeof(float), this, onChanged));

    public override string ToString() => $"{nameof(Float)} {address}";

    private readonly Logo logo;
    private readonly int address;
  }
}