using System;

namespace Hermod.EmailImport.Tests {

    using Data;

    [TestClass]
    public class JsonDbConnectorTests {

        static byte[] EncryptionKey { get; }
        static byte[] EncryptionIv { get; }
        static FileInfo JsonFile { get; }

        static JsonDbConnectorTests() {
            byte[] encryptionKey;
            byte[] encryptionIv;
            JsonDatabaseConnector.GenerateNewAesKey(out encryptionKey, out encryptionIv);
            EncryptionIv = encryptionIv;
            EncryptionKey = encryptionKey;

            JsonFile = new FileInfo(Path.GetTempFileName() + ".json");
        }

        public const string HelloTest = "Hello123";
        public byte[] EncryptedHello { get; private set; } = Array.Empty<byte>();

        [TestMethod]
        public void TestSyncEncryption() {
            var dbConn = new JsonDatabaseConnector(JsonFile, EncryptionKey, EncryptionIv);
            EncryptedHello = dbConn.EncryptString("Hello123");
            Assert.IsNotNull(EncryptedHello);
            Assert.IsTrue(EncryptedHello.Length > 0);
        }

        [TestMethod]
        public void TestSyncDecryption() {
            if (EncryptedHello is null || EncryptedHello.Length == 0) { TestSyncEncryption(); }

            var dbConn = new JsonDatabaseConnector(JsonFile, EncryptionKey, EncryptionIv);
            Assert.IsNotNull(EncryptedHello);
            Assert.IsTrue(EncryptedHello.Length > 0);
            var decryptedHello = dbConn.DecryptString(EncryptedHello);
            Assert.IsNotNull(decryptedHello);
            Assert.IsFalse(string.IsNullOrEmpty(decryptedHello));
            Assert.IsFalse(string.IsNullOrWhiteSpace(decryptedHello));
            Assert.AreEqual(HelloTest, decryptedHello);
        }

        [TestMethod]
        public async Task TestAsyncEncryption() {
            var dbConn = new JsonDatabaseConnector(JsonFile, EncryptionKey, EncryptionIv);
            EncryptedHello = await dbConn.EncryptStringAsync("Hello123");
            Assert.IsNotNull(EncryptedHello);
            Assert.IsTrue(EncryptedHello.Length > 0);
        }

        [TestMethod]
        public async Task TestAsyncDecryption() {
            if (EncryptedHello is null || EncryptedHello.Length == 0) { TestSyncEncryption(); }

            var dbConn = new JsonDatabaseConnector(JsonFile, EncryptionKey, EncryptionIv);
            Assert.IsNotNull(EncryptedHello);
            Assert.IsTrue(EncryptedHello.Length > 0);
            var decryptedHello = await dbConn.DecryptStringAsync(EncryptedHello);
            Assert.IsNotNull(decryptedHello);
            Assert.IsFalse(string.IsNullOrEmpty(decryptedHello));
            Assert.IsFalse(string.IsNullOrWhiteSpace(decryptedHello));
            Assert.AreEqual(HelloTest, decryptedHello);
        }

    }
}

