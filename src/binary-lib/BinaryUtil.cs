using System.Linq;

namespace binary_lib
{
    public static class BinaryUtil
    {        
        /// <summary>
        /// Converts an array of bytes to a Hex string.
        /// There is no prefix.
        /// All Hex digits are upper case.
        /// e.g. { 123, 234 } => "78EA"
        /// </summary>
        /// <param name="data">An array of bytes</param>
        /// <returns>The bytes represented as a hex string.</returns>
        public static string ByteArrayToHexString(byte[] data)
        {
            return string.Join(string.Empty, data.Select(queryHashedByte => string.Format("{0:x2}", queryHashedByte)));
        }
    }
}
