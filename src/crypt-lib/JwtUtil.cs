using System;
using System.Security.Cryptography;
using System.Text;

namespace crypt_lib
{
    public static class JwtUtil
    {
        public static string Encode(string payload, string privateKey)
        {
            var headerText = "{\"alg\":\"HS256\"}";
            var headerBytes = Encoding.Default.GetBytes(headerText);
            var base64Header = Convert.ToBase64String(headerBytes);

            var payloadBytes = Encoding.Default.GetBytes(payload);
            var base64Payload = Convert.ToBase64String(payloadBytes);

            var apiSecretBytes = Encoding.Default.GetBytes(privateKey);

            var combo = $"{base64Header}.{base64Payload}";
            var comboBytes = Encoding.Default.GetBytes(combo);

            var signatureBytes = new HMACSHA256(apiSecretBytes).ComputeHash(comboBytes);
            var base64Signature = Convert.ToBase64String(signatureBytes, Base64FormattingOptions.None);

            return $"{base64Header}.{base64Payload}.{base64Signature}";
        }
    }
}
