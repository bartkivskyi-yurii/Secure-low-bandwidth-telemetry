using System;
using System.Linq;
using System.Security.Cryptography;

namespace TelemetryLink
{
    public class Security
    {
        public byte[] EncryptAndSign(byte[] plaintext, byte[] encryptionKey, byte[] authKey)
        {
            byte[] iv;
            byte[] ciphertext;

            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.GenerateIV();
                iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
                }
            }

            byte[] ivAndCiphertext = iv.Concat(ciphertext).ToArray();

            using (HMACSHA256 hmac = new HMACSHA256(authKey))
            {
                byte[] tag = hmac.ComputeHash(ivAndCiphertext);
                return ivAndCiphertext.Concat(tag).ToArray();
            }
        }
    }
}