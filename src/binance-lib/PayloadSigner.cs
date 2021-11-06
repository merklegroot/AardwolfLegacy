using System.Security.Cryptography;
using System.Text;

namespace binance_lib
{
    public class PayloadSigner
    {
        private readonly Encoding _encoding = Encoding.Default;
        private readonly HMACSHA256 _sha;

        public PayloadSigner(string key)
        {
            var keyBytes = _encoding.GetBytes(key);
            _sha = new HMACSHA256(keyBytes);
        }

        public string Sign(string payload)
        {
            var payloadBytes = _encoding.GetBytes(payload);
            var signatureBytes = _sha.ComputeHash(payloadBytes);
            var signatureText = _encoding.GetString(signatureBytes);

            return signatureText;
        }
    }
}
