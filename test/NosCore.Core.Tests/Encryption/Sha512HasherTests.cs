//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Encryption;
using SpecLight;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Core.Tests.Encryption
{
    [TestClass]
    public class Sha512HasherTests
    {
        private readonly Sha512Hasher Hasher = new();
        private string First = null!;
        private string Second = null!;

        [TestMethod]
        public async Task SaltedHashCoversSaltThenPassword()
        {
            await new Spec("Salted hash covers salt then password")
                .When(HashingPassword_WithSalt_, "secret", "pepper")
                .Then(TheHashShouldBeSha512Of_, "peppersecret")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SaltedHashDependsOnThePassword()
        {
            await new Spec("Salted hash depends on the password")
                .When(HashingTwoPasswordsWithTheSameSalt)
                .Then(TheHashesShouldDiffer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnsaltedHashIsSha512OfThePassword()
        {
            await new Spec("Unsalted hash is sha512 of the password")
                .When(HashingPassword_WithoutSalt, "secret")
                .Then(TheHashShouldBeSha512Of_, "secret")
                .ExecuteAsync();
        }

        private void HashingPassword_WithSalt_(string password, string salt) => First = Hasher.Hash(password, salt);

        private void HashingPassword_WithoutSalt(string password) => First = Hasher.Hash(password);

        private void HashingTwoPasswordsWithTheSameSalt()
        {
            First = Hasher.Hash("secret", "pepper");
            Second = Hasher.Hash("other", "pepper");
        }

        private void TheHashShouldBeSha512Of_(string input) =>
            Assert.AreEqual(Convert.ToHexStringLower(SHA512.HashData(Encoding.UTF8.GetBytes(input))), First);

        private void TheHashesShouldDiffer() => Assert.AreNotEqual(First, Second);
    }
}
