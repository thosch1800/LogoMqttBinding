using System.Globalization;
using System.Linq;
using System.Text;

namespace LogoMqttBinding.MqttAdapter
{
  internal class MqttFormat
  {
    public static bool ToValue(byte[]? payload, out byte result)
    {
      var s = Decode(payload);
      return byte.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static bool ToValue(byte[]? payload, out short result)
    {
      var s = Decode(payload);
      return short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static bool ToValue(byte[]? payload, out float result)
    {
      var s = Decode(payload);
      return float.TryParse(Decode(payload), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }



    public static byte[] ToPayload(byte value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encode(s);
    }

    public static byte[] ToPayload(short value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encode(s);
    }

    public static byte[] ToPayload(float value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return Encode(s);
    }



    public static string Decode(byte[]? payload) => payload == null ? "<null>" : Encoding.UTF8.GetString(payload);
    public static byte[] Encode(string s) => Encoding.UTF8.GetBytes(s);
    public static string AsByteString(byte[]? payload) => payload != null ? string.Join("-", payload.Select(b => b.ToString("X"))) : "<null>";
  }
}