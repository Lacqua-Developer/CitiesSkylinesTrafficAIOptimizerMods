using NUnit.Framework;
using TrafficAIOptimizer.Security;

namespace TrafficAIOptimizer.Tests.Security
{
    [TestFixture]
    public class SecureKeyStorageTests
    {
        [SetUp]
        public void Setup()
        {
            SecureKeyStorage.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SecureKeyStorage.Clear();
        }

        [Test]
        public void GivenEmptyStorage_WhenChecked_ThenHasKeyShouldBeFalse()
        {
            Assert.IsFalse(SecureKeyStorage.HasKey);
            Assert.IsNull(SecureKeyStorage.GetKeyUnsafe());
            Assert.That(SecureKeyStorage.GetMaskedDisplay(), Does.Contain("Nenhuma chave definida"));
        }

        [Test]
        public void GivenValidKey_WhenSet_ThenHasKeyShouldBeTrueAndKeyStoredTemporarily()
        {
            string secret = "sk-deepseek-secret-123456789";
            SecureKeyStorage.SetKey(secret);

            Assert.IsTrue(SecureKeyStorage.HasKey);
            Assert.AreEqual(secret, SecureKeyStorage.GetKeyUnsafe());
        }

        [Test]
        public void GivenValidKey_WhenDisplayMasked_ThenRawKeyShouldNotBeExposed()
        {
            string secret = "sk-deepseek-secret-123456789";
            SecureKeyStorage.SetKey(secret);

            string masked = SecureKeyStorage.GetMaskedDisplay();

            Assert.IsNotNull(masked);
            Assert.That(masked, Does.Contain("sk-"));
            Assert.That(masked, Does.Contain("6789"));
            Assert.That(masked, Does.Not.Contain("deepseek-secret"));
        }

        [Test]
        public void GivenKeyInRAM_WhenClearInvoked_ThenMemoryMustBeWiped()
        {
            SecureKeyStorage.SetKey("sk-sample-super-secret-key");
            Assert.IsTrue(SecureKeyStorage.HasKey);

            SecureKeyStorage.Clear();

            Assert.IsFalse(SecureKeyStorage.HasKey);
            Assert.IsNull(SecureKeyStorage.GetKeyUnsafe());
        }
    }
}
