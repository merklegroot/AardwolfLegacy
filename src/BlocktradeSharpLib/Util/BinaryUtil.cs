using System.Text;

namespace BlocktradeExchangeLib.Util
{
    public static class BinaryUtil
    {
        public static string HexEncode(byte[] data)
        {
            var hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data) { hex.AppendFormat("{0:x2}", b); }

            return hex.ToString();
        }
    }
}
