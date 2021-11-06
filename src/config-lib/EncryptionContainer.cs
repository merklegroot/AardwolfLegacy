using crypt_lib;
using System;
using System.Security.Cryptography;

namespace config_lib
{
    public class EncryptionContainer
    {
        public byte[] IV { get; set; }
        public byte[] Encrypted { get; set; }

        public EncryptionContainer() { }

        public static EncryptionContainer Create(byte[] key, string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) { return new EncryptionContainer(); }

            byte[] iv;
            byte[] encrypted;
            using (var crypt = new RijndaelManaged())
            {
                crypt.Key = key;
                crypt.GenerateIV();
                iv = crypt.IV;

                encrypted = SymmUtil.EncryptStringToBytes(plainText, crypt.Key, crypt.IV);
            }

            return new EncryptionContainer { IV = iv, Encrypted = encrypted };
        }

        public string Decrypt(byte[] key)
        {
            if (key == null || key.Length == 0) { throw new ArgumentNullException(nameof(key)); }
            return SymmUtil.DecryptStringFromBytes(Encrypted, key, IV);
        }
    }
}
