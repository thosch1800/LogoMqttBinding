using System;
using System.Globalization;

namespace LogoMqttBinding.Status
{
  public class LastNotification
  {
    public static implicit operator string(LastNotification instance) => DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
  }
}