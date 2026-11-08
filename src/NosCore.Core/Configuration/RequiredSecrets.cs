//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosCore.Core.Configuration
{
    public sealed record SecretSetting(string Key, string EnvironmentVariable, int MinimumLength = 1);

    public static class RequiredSecrets
    {
        public const string Placeholder = "CHANGE_ME";

        // HMAC-SHA256 refuses keys shorter than 256 bits.
        public const int JwtSigningKeyMinimumLength = 32;

        private static readonly HashSet<string> ShippedDefaults = new(StringComparer.OrdinalIgnoreCase)
        {
            Placeholder, "password", "postgres", "guest", "NosCorePassword"
        };

        public static readonly SecretSetting DatabasePassword = new("Database:Password", "DB_PASSWORD");
        public static readonly SecretSetting JwtSigningKey = new("Jwt:SigningKey", "JWT_SIGNING_KEY", JwtSigningKeyMinimumLength);
        public static readonly SecretSetting MallSignatureKey = new("Mall:SignatureKey", "MALL_SIGNATURE_KEY");

        public static SecretSetting MasterPassword(string section) => new($"{section}:Password", "MASTER_PASSWORD");

        public static IReadOnlyList<string> FindMissing(IConfiguration configuration, params SecretSetting[] settings)
        {
            var missing = settings
                .Where(setting => !IsUsable(configuration[setting.Key], setting.MinimumLength))
                .Select(setting => setting.MinimumLength > 1
                    ? $"{setting.Key} must be set to a value of at least {setting.MinimumLength} characters that is not a shipped default. Set the {setting.EnvironmentVariable} environment variable."
                    : $"{setting.Key} must be set to a value that is not a shipped default. Set the {setting.EnvironmentVariable} environment variable.")
                .ToList();

            if (string.Equals(configuration["Messaging:Transport"], "RabbitMq", StringComparison.OrdinalIgnoreCase)
                && !IsUsable(BrokerPassword(configuration["Messaging:RabbitMqUri"]), 1))
            {
                missing.Add("Messaging:RabbitMqUri must carry broker credentials that are not a shipped default. Set the RABBITMQ_URI environment variable to amqp://user:password@host:port.");
            }

            return missing;
        }

        public static void EnsureSet(IConfiguration configuration, params SecretSetting[] settings)
        {
            var missing = FindMissing(configuration, settings);
            if (missing.Count == 0)
            {
                return;
            }

            foreach (var message in missing)
            {
                Log.Fatal(message);
            }

            var builder = new StringBuilder("Refusing to start until every secret is configured:");
            foreach (var message in missing)
            {
                builder.AppendLine().Append(message);
            }

            throw new InvalidOperationException(builder.ToString());
        }

        private static bool IsUsable(string? value, int minimumLength) =>
            !string.IsNullOrWhiteSpace(value) && value.Length >= minimumLength && !ShippedDefaults.Contains(value);

        private static string? BrokerPassword(string? uri)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            {
                return null;
            }

            var separator = parsed.UserInfo.IndexOf(':');
            return separator < 0 ? null : Uri.UnescapeDataString(parsed.UserInfo[(separator + 1)..]);
        }
    }
}
