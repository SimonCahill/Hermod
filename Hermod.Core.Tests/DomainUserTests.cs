using System;

namespace Hermod.Core.Tests {

    using Accounts;

    using System.Text;

    [TestClass]
    public class DomainUserTests {

        [TestMethod]
        public void TestGenerateEntropy_NullReference() {
            // this will generate two array of DomainUser.SaltSize length and will compare them
            byte[] arr1 = null;
            byte[] arr2 = null;

            DomainUser.GenerateEntropy(ref arr1);
            DomainUser.GenerateEntropy(ref arr2);

            Assert.IsNotNull(arr1);
            Assert.IsNotNull(arr2);
            Assert.IsTrue(arr1.Length == DomainUser.SaltSize);
            Assert.IsTrue(arr2.Length == DomainUser.SaltSize);
            Assert.AreNotEqual(arr1, arr2);
        }

        [TestMethod]
        public void TestGenerateEntropy_ArraySmaller() {
            byte[] arr1 = new byte[10];
            byte[] arr2 = new byte[28];

            DomainUser.GenerateEntropy(ref arr1);
            DomainUser.GenerateEntropy(ref arr2);

            Assert.IsNotNull(arr1);
            Assert.IsNotNull(arr2);
            Assert.IsTrue(arr1.Length == DomainUser.SaltSize);
            Assert.IsTrue(arr2.Length == DomainUser.SaltSize);
            Assert.AreNotEqual(arr1, arr2);
        }

        [TestMethod]
        public void TestGenerateEntropy_ArrayBigger() {
            byte[] arr1 = new byte[DomainUser.SaltSize * 3];
            byte[] arr2 = new byte[DomainUser.SaltSize * 2];

            DomainUser.GenerateEntropy(ref arr1);
            DomainUser.GenerateEntropy(ref arr2);

            Assert.IsNotNull(arr1);
            Assert.IsNotNull(arr2);
            Assert.IsTrue(arr1.Length == DomainUser.SaltSize);
            Assert.IsTrue(arr2.Length == DomainUser.SaltSize);
            Assert.AreNotEqual(arr1, arr2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInstantiation_NullSalt() {
            new DomainUser(0, "xczvuzlbin", Encoding.UTF8.GetBytes("ftzugihöo"), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInstantiation_TooShortSalt() {
            new DomainUser(0, "xczvuzlbin", Encoding.UTF8.GetBytes("ftzugihöo"), new byte[30]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInstantiation_TooLongSalt() {
            new DomainUser(0, "xczvuzlbin", Encoding.UTF8.GetBytes("ftzugihöo"), new byte[DomainUser.SaltSize * 4]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInstantiation_SaltFilledWithZeros() {
            var array = new byte[DomainUser.SaltSize];
            Array.Fill<byte>(array, 0);

            new DomainUser(0, "xczvuzlbin", Encoding.UTF8.GetBytes("ftzugihöo"), array);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInstantiation_SaltFilledWithIdenticalNonZeros() {
            var array = new byte[DomainUser.SaltSize];
            Array.Fill<byte>(array, 0xe4);

            new DomainUser(0, "xczvuzlbin", Encoding.UTF8.GetBytes("ftzugihöo"), array);
        }
    }
}

