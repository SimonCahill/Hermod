namespace Hermod.Config.Tests {

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class ConfigManagerTests: ConfigManager {


        [TestMethod]
        public void TestLoadDefaultConfigs() {
            var cfg = LoadDefaultConfig();
            Assert.IsTrue(cfg.Count > 0);
        }

        [TestMethod]
        public void TestLoadConfigs_FileDoesntExist() {
            m_configDictionary = new Newtonsoft.Json.Linq.JObject();
            Assert.IsTrue(m_configDictionary.Count == 0);
            ConfigFile = new FileInfo(Path.GetTempFileName());
            Assert.IsTrue(m_configDictionary.Count > 0);
        }

        [TestMethod]
        public void TestGetConfig_SimpleAccess() {
            m_configDictionary = JObject.Parse("""{ "TestConfig": 123, "AnotherTest": "Test123" }""");
            var testConfig = GetConfig<int>("TestConfig");
            Assert.AreEqual(123, testConfig);
            var anotherTest = GetConfig<string>("AnotherTest");
            Assert.AreEqual("Test123", anotherTest);
        }

        [TestMethod]
        public void TestGetConfig_DotNotation() {
            m_configDictionary = JObject.Parse("""{ "TestConfig": { "SubTest": 123, "SubTest2": { "SubTest3": true } }, "AnotherTest": "Test123" }""");

            Assert.IsNotNull(GetConfig<object?>("TestConfig"));
            Assert.IsInstanceOfType(GetConfig<object?>("TestConfig"), typeof(Object));
            Assert.AreEqual(GetConfig<string>("AnotherTest"), "Test123");

            Assert.AreEqual(GetConfig<int>("TestConfig.SubTest"), 123);
            Assert.IsNotNull(GetConfig<object?>("TestConfig.SubTest2"));
            Assert.IsInstanceOfType(GetConfig<object?>("TestConfig.SubTest2"), typeof(Object));
            Assert.IsTrue(GetConfig<bool>("TestConfig.SubTest2.SubTest3"));

        }
    }

}