using System.Security.Cryptography;
using System.Text;

namespace crypt_lib
{
    /// <summary>
    /// Friends don't let friends use MD5.
    /// Sometimes, those friends just won't listen and that makes me sad. :(
    /// This util exists because sometimes you're not the person who gets to make that decision.
    /// (Sersiouly though, MD5 is not secure, so stop using it!)
    /// </summary>
    public static class Md5Util
    {
        public static byte[] GetMd5Hash(string plainText) => GetMd5Hash(plainText, Encoding.Default);

        public static byte[] GetMd5Hash(string plainText, Encoding encoding) => 
            plainText != null
                ? new MD5CryptoServiceProvider().ComputeHash(encoding.GetBytes(plainText))
                : null;
    }
}
