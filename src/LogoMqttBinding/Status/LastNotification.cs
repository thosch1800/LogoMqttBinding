using System;
using System.Globalization;

namespace LogoMqttBinding.Status
{
  public class LastNotification
  {
    public static implicit operator string(LastNotification _) => DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
  }
}