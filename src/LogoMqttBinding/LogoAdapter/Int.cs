using System;
using Sharp7;

namespace LogoMqttBinding.LogoAdapter
{
  internal class Int : ILogoVariable<short>
  {
    public Int(Logo logo, int address)
    {
      if (address < 0) throw new ArgumentOutOfRangeException(nameof(address), "must be a positive number");
      this.logo = logo ?? throw new ArgumentNullException(nameof(logo));
      this.address = address;
    }

    public short Get()
    {
      var cached = logo.GetBytes(address, sizeof(short));
      var result = cached.GetIntAt(0);
      return (short) result;
    }

    public void Set(short value)
    {
      var buffer = new byte[sizeof(short)];
      buffer.SetIntAt(0, value);
      logo.Execute(c => c.DBWrite(1, address, sizeof(short), buffer));
    }

    public NotificationContext SubscribeToChangeNotification(Action<ILogoVariable<short>> onChanged)
      => logo.SubscribeToChangeNotification(
        new NotificationContext<short>(address, sizeof(short), this, onChanged));

    private readonly Logo logo;
    private readonly int address;
  }
}