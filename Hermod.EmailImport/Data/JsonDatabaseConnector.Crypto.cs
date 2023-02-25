using System;

namespace Hermod.EmailImport.Data {

    using System.Security.Cryptography;

    partial class JsonDatabaseConnector {

        private byte[] m_encKey;
        private byte[] m_initVector;

        private string DecryptString(byte[] bytes) {
            using var aes = Aes.Create();

            aes.IV = m_initVector;
            aes.Key = m_encKey;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var memStream = new MemoryStream(bytes);
            using var cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read);
            using var sReader = new StreamReader(cryptoStream);

            return sReader.ReadToEnd();
        }

        private async Task<string> DecryptStringAsync(byte[] bytes) {
            using var aes = Aes.Create();

            aes.IV = m_initVector;
            aes.Key = m_encKey;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var memStream = new MemoryStream(bytes);
            using var cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read);
            using var sReader = new StreamReader(cryptoStream);

            return await sReader.ReadToEndAsync();
        }

        private byte[] EncryptString(string plaintext) {
            using var aes = Aes.Create();

            aes.IV = m_initVector;
            aes.Key = m_encKey;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write);
            using var swriter = new StreamWriter(cryptoStream);

            swriter.Write(plaintext);

            return memStream.ToArray();
        }

        private async Task<byte[]> EncryptStringAsync(string plaintext) {
            using var aes = Aes.Create();

            aes.IV = m_initVector;
            aes.Key = m_encKey;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write);
            using var swriter = new StreamWriter(cryptoStream);

            await swriter.WriteAsync(plaintext);

            return memStream.ToArray();
        }

        public static void GenerateNewAesKey(out byte[] key, out byte[] initVector) {
            using var aes = Aes.Create();
            key = aes.Key;
            initVector = aes.IV;
        }

    }
}

