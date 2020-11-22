using System.Globalization;
using System.Text;

namespace LogoMqttBinding.MqttAdapter
{
  internal class MqttFormat
  {
    public static bool ToValue(byte[]? payload, out byte result)
    {
      var s = EncodingToString(payload);
      return byte.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static bool ToValue(byte[]? payload, out short result)
    {
      var s = EncodingToString(payload);
      return short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static bool ToValue(byte[]? payload, out float result)
    {
      var s = EncodingToString(payload);
      return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }



    public static byte[] ToPayload(byte value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return EncodingToBytes(s);
    }

    public static byte[] ToPayload(short value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return EncodingToBytes(s);
    }

    public static byte[] ToPayload(float value)
    {
      var s = value.ToString(CultureInfo.InvariantCulture);
      return EncodingToBytes(s);
    }



    private static string EncodingToString(byte[]? payload)
      => payload == null
        ? "<null>"
        : Encoding.UTF8.GetString(payload);

    private static byte[] EncodingToBytes(string s)
      => Encoding.UTF8.GetBytes(s);
  }
}