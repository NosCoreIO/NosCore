//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Core.Tests
{
    [TestClass]
    public class RequiredSecretsTests
    {
        private const string SigningKey = "a-signing-key-that-is-long-enough-for-hmac";

        private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
                .Build();

        private static IConfiguration ConfiguredApi(params (string Key, string? Value)[] overrides) =>
            Configuration(new[]
                {
                    ("MasterCommunication:Password", (string?)"a-master-password"),
                    ("Database:Password", "a-database-password"),
                    ("Jwt:SigningKey", SigningKey),
                    ("Mall:SignatureKey", "a-mall-key"),
                }
                .Where(v => overrides.All(o => o.Key != v.Item1))
                .Concat(overrides)
                .ToArray());

        private static readonly SecretSetting[] ApiSecrets =
        [
            RequiredSecrets.MasterPassword("MasterCommunication"),
            RequiredSecrets.DatabasePassword,
            RequiredSecrets.JwtSigningKey,
            RequiredSecrets.MallSignatureKey
        ];

        [TestMethod]
        public void StartsWhenEverySecretIsSet()
        {
            RequiredSecrets.EnsureSet(ConfiguredApi(), ApiSecrets);
        }

        [DataTestMethod]
        [DataRow("Database:Password", "", "DB_PASSWORD")]
        [DataRow("Database:Password", "password", "DB_PASSWORD")]
        [DataRow("Database:Password", "CHANGE_ME", "DB_PASSWORD")]
        [DataRow("MasterCommunication:Password", "NosCorePassword", "MASTER_PASSWORD")]
        [DataRow("MasterCommunication:Password", null, "MASTER_PASSWORD")]
        [DataRow("Jwt:SigningKey", "CHANGE_ME", "JWT_SIGNING_KEY")]
        [DataRow("Jwt:SigningKey", "too-short-for-hmac", "JWT_SIGNING_KEY")]
        [DataRow("Mall:SignatureKey", " ", "MALL_SIGNATURE_KEY")]
        public void RefusesToStartWhenASecretIsUnsetOrAShippedDefault(string key, string? value, string variable)
        {
            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => RequiredSecrets.EnsureSet(ConfiguredApi((key, value)), ApiSecrets));

            StringAssert.Contains(exception.Message, key);
            StringAssert.Contains(exception.Message, variable);
        }

        [DataTestMethod]
        [DataRow("amqp://guest:guest@localhost:5672")]
        [DataRow("amqp://localhost:5672")]
        [DataRow("amqp://CHANGE_ME:CHANGE_ME@localhost:5672")]
        public void RefusesToStartWhenTheBrokerUsesShippedCredentials(string uri)
        {
            var configuration = ConfiguredApi(("Messaging:Transport", "RabbitMq"), ("Messaging:RabbitMqUri", uri));

            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => RequiredSecrets.EnsureSet(configuration, ApiSecrets));

            StringAssert.Contains(exception.Message, "RABBITMQ_URI");
        }

        [TestMethod]
        public void StartsWhenTheBrokerHasItsOwnCredentials()
        {
            RequiredSecrets.EnsureSet(ConfiguredApi(("Messaging:Transport", "RabbitMq"),
                ("Messaging:RabbitMqUri", "amqp://noscore:a-broker-password@rabbitmq:5672")), ApiSecrets);
        }

        [TestMethod]
        public void IgnoresTheBrokerUriWhenMessagingGoesThroughPostgresql()
        {
            RequiredSecrets.EnsureSet(ConfiguredApi(("Messaging:Transport", "Postgresql"),
                ("Messaging:RabbitMqUri", "amqp://guest:guest@localhost:5672")), ApiSecrets);
        }

        [TestMethod]
        public void ListsEveryMissingSecretAtOnce()
        {
            var missing = RequiredSecrets.FindMissing(Configuration(), ApiSecrets);

            CollectionAssert.AreEquivalent(
                new[] { "MasterCommunication:Password", "Database:Password", "Jwt:SigningKey", "Mall:SignatureKey" },
                missing.Select(m => m.Split(' ')[0]).ToArray());
        }
    }
}
