using System;
using LogoMqttBinding.LogoAdapter.Interfaces;

namespace LogoMqttBinding.LogoAdapter
{
  internal class Byte : ILogoVariable<byte>
  {
    public Byte(Logo logo, int address)
    {
      if (address < 0) throw new ArgumentOutOfRangeException(nameof(address), "must be a positive number");
      this.logo = logo ?? throw new ArgumentNullException(nameof(logo));
      this.address = address;
    }

    public byte Get()
    {
      var cached = logo.GetBytes(address, sizeof(byte));
      var result = cached[0];
      return result;
    }

    public void Set(byte value)
    {
      var buffer = new[] { value };
      logo.Execute(c => c.DBWrite(1, address, sizeof(byte), buffer));
    }

    public NotificationContext SubscribeToChangeNotification(Action<ILogoVariable<byte>> onChanged)
      => logo.SubscribeToChangeNotification(
        new NotificationContext<byte>(address, sizeof(byte), this, onChanged));

    public override string ToString() => $"{nameof(Byte)} {address}";

    private readonly Logo logo;
    private readonly int address;
  }
}