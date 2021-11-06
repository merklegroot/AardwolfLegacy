using System;
using System.IO;
using System.Security.Cryptography;

namespace crypt_lib
{
    // https://stackoverflow.com/questions/273452/using-aes-encryption-in-c-sharp
    // http://msdn.microsoft.com/en-us/library/system.security.cryptography.rijndaelmanaged.aspx
    public static class SymmUtil
    {
        private static Random _random = new Random();

        public static byte[] EncryptStringToBytes(string plainText, byte[] key, byte[] initializationVector)
        {
            if (plainText == null || plainText.Length <= 0) { throw new ArgumentNullException(nameof(plainText)); }
            if (key == null || key.Length <= 0) { throw new ArgumentNullException(nameof(key)); }
            if (initializationVector == null || initializationVector.Length <= 0) { throw new ArgumentNullException(nameof(initializationVector)); }

            byte[] encrypted;
            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged crypt = new RijndaelManaged())
            {
                crypt.Key = key;
                crypt.IV = initializationVector;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform encryptor = crypt.CreateEncryptor(crypt.Key, crypt.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return encrypted;
        }

        public static string EncryptStringToBase64String(string plainText, byte[] Key, byte[] IV)
        {
            var cipherBytes = EncryptStringToBytes(plainText, Key, IV);
            if (cipherBytes == null || cipherBytes.Length == 0) { return null; }

            return Convert.ToBase64String(cipherBytes);
        }

        public static string DecryptStringFromBytes(byte[] cipherBytes, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (cipherBytes == null || cipherBytes.Length <= 0) { throw new ArgumentNullException("cipherText"); }
            if (Key == null || Key.Length <= 0) { throw new ArgumentNullException("Key"); }
            if (IV == null || IV.Length <= 0) { throw new ArgumentNullException("IV"); }

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;

        }

        public static string DecryptStringFromBase64String(string cipher64, byte[] Key, byte[] IV)
        {
            if (cipher64 == null || cipher64.Length == 0) { return cipher64; }
            var cipherBytes = Convert.FromBase64String(cipher64);

            return DecryptStringFromBytes(cipherBytes, Key, IV);
        }
    }
}
