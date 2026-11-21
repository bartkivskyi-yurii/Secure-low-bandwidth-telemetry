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

        public byte[] VerifyAndDecrypt(byte[] securePacket, byte[] encryptionKey, byte[] authKey)
        {
            if (securePacket.Length < 48)
                throw new CryptographicException("Packet too short.");

            int tagLength = 32;
            int ivLength = 16;
            int ciphertextLength = securePacket.Length - ivLength - tagLength;

            byte[] ivAndCiphertext = securePacket.Take(ivLength + ciphertextLength).ToArray();
            byte[] receivedTag = securePacket.Skip(ivLength + ciphertextLength).Take(tagLength).ToArray();

            using (HMACSHA256 hmac = new HMACSHA256(authKey))
            {
                byte[] computedTag = hmac.ComputeHash(ivAndCiphertext);
                if (!CryptographicOperations.FixedTimeEquals(receivedTag, computedTag))
                {
                    throw new CryptographicException("HMAC validation failed! Data tampered.");
                }
            }

            byte[] iv = ivAndCiphertext.Take(ivLength).ToArray();
            byte[] ciphertext = ivAndCiphertext.Skip(ivLength).ToArray();

            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                }
            }
        }
        public byte[] SimulateMitmAttack(byte[] packet)
        {
            byte[] tampered = (byte[])packet.Clone();
            if (tampered.Length > 20)
            {
                tampered[20] ^= 0xFF;
            }
            return tampered;
        }
    }
}