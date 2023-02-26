using System;

namespace Hermod.EmailImport.Data {
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;

    partial class JsonDatabaseConnector {

        private byte[] m_encKey;
        private byte[] m_initVector;

        /// <summary>
        /// Decrypts a cipher text to a string.
        /// </summary>
        /// <param name="bytes">The cipher text to decrypt.</param>
        /// <returns>The decrypted string.</returns>
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

        /// <summary>
        /// Asynchronously decrypts a cipher text to a string.
        /// </summary>
        /// <param name="bytes">The cipher text to decrypt.</param>
        /// <returns>The decrypted string.</returns>
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

        /// <summary>
        /// Encrypts a string.
        /// </summary>
        /// <param name="plaintext">The plaintext string to encrypt.</param>
        /// <returns>The encrypted string as a series of bytes.</returns>
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

        /// <summary>
        /// Asynchronously encrypts a given string.
        /// </summary>
        /// <param name="plaintext">The plaintext string to encrypt.</param>
        /// <returns>An awaitable <see cref="Task{Byte[]}"/> containing the encrypted data.</returns>
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

        /// <summary>
        /// Generates a new AES encryption key with init vector.
        /// </summary>
        /// <param name="key">Out param; the new encryption key.</param>
        /// <param name="initVector">Out param; the new init vector (IV)</param>
        public static void GenerateNewAesKey(out byte[] key, out byte[] initVector) {
            using var aes = Aes.Create();
            key = aes.Key;
            initVector = aes.IV;
        }

        /// <summary>
        /// Ensures all data is dumped to the JSON before the resources are freed.
        /// </summary>
        public override void Dispose() => DumpJson();

        /// <summary>
        /// Asynchronously ensures all data is dumped to the JSON before the resources are freed.
        /// </summary>
        public async Task DisposeAsync() => await DumpJsonAsync();

    }
}

