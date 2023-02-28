using System;

namespace Hermod.Core.Tests {

    using Accounts;

    [TestClass]
    public class DomainTests {

        [TestMethod]
        public void TestIsValidDomain_InvalidDomains() {
            Assert.IsFalse(Domain.IsValidDomain(string.Empty, out var tldLevels, out var tld, out var domain));
            Assert.IsNull(tldLevels);
            Assert.IsNull(tld);
            Assert.IsNull(domain);

            Assert.IsFalse(Domain.IsValidDomain(null, out tldLevels, out tld, out domain));
            Assert.IsNull(tldLevels);
            Assert.IsNull(tld);
            Assert.IsNull(domain);

            Assert.IsFalse(Domain.IsValidDomain(new string(' ', 256), out tldLevels, out tld, out domain));
            Assert.IsNull(tldLevels);
            Assert.IsNull(tld);
            Assert.IsNull(domain);

            Assert.IsFalse(Domain.IsValidDomain("notarealdomain", out tldLevels, out tld, out domain));
            Assert.IsNull(tldLevels);
            Assert.IsNull(tld);
            Assert.IsNull(domain);

            Assert.IsFalse(Domain.IsValidDomain("still,notreal", out tldLevels, out tld, out domain));
            Assert.IsNull(tldLevels);
            Assert.IsNull(tld);
            Assert.IsNull(domain);

            Assert.IsFalse(Domain.IsValidDomain("example.org,broken", out tldLevels, out tld, out domain));
            Assert.IsNull(tldLevels);
            Assert.IsNull(tld);
            Assert.IsNull(domain);

            Assert.IsFalse(Domain.IsValidDomain("https://example.org", out tldLevels, out tld, out domain));
            Assert.IsNull(tldLevels);
            Assert.IsNull(tld);
            Assert.IsNull(domain);
        }

        [TestMethod]
        public void TestIsValidDomain_SimpleDomain() {
            Assert.IsTrue(Domain.IsValidDomain("org.example", out var tldLevels, out var tld, out var domain));
            Assert.IsNotNull(tldLevels);
            Assert.AreEqual(1, tldLevels);
            Assert.IsNotNull(tld);
            Assert.AreEqual("org", tld);
            Assert.IsNotNull(domain);
            Assert.AreEqual("example", domain);
        }

        [TestMethod]
        public void TestIsValidDomain_MultiLevelTld() {
            Assert.IsTrue(Domain.IsValidDomain("org.uk.example", out var tldLevels, out var tld, out var domain));
            Assert.IsNotNull(tldLevels);
            Assert.AreEqual(2, tldLevels);
            Assert.IsNotNull(tld);
            Assert.AreEqual("org.uk", tld);
            Assert.IsNotNull(domain);
            Assert.AreEqual("example", domain);
        }

        [TestMethod]
        public void TestIsValidDomain_MultiLevelDomain() {
            Assert.IsTrue(Domain.IsValidDomain("org.example.test", out var tldLevels, out var tld, out var domain));
            Assert.IsNotNull(tldLevels);
            Assert.AreEqual(1, tldLevels);
            Assert.IsNotNull(tld);
            Assert.AreEqual("org", tld);
            Assert.IsNotNull(domain);
            Assert.AreEqual("example.test", domain);
        }

        [TestMethod]
        public void TestIsValidDomain_MultiLevelTldAndDomain() {
            Assert.IsTrue(Domain.IsValidDomain("org.uk.example.test", out var tldLevels, out var tld, out var domain));
            Assert.IsNotNull(tldLevels);
            Assert.AreEqual(2, tldLevels);
            Assert.IsNotNull(tld);
            Assert.AreEqual("org.uk", tld);
            Assert.IsNotNull(domain);
            Assert.AreEqual("example.test", domain);
        }

        [TestMethod]
        public void TestIsValidDomain_AllowNonValidDomains() {
            Assert.IsFalse(Domain.IsValidDomain("home.local.test.example", out var tldLevels, out var tld, out var domain));
            Assert.IsNotNull(tldLevels);
            Assert.AreEqual(1, tldLevels);
            Assert.IsNotNull(tld);
            Assert.AreEqual("home", tld);
            Assert.IsNotNull(domain);
            Assert.AreEqual("local.test.example", domain);
        }

    }
}