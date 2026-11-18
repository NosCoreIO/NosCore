//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

// In the root namespace on purpose: code anywhere under NosCore finds these overloads before the
// string ones it imports, so every logLanguage[key] call carries the key's event id unchanged.
namespace NosCore
{
    public static class LocalizedLoggerExtensions
    {
        public static void LogTrace(this ILogger logger, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Trace, null, message, args);

        public static void LogTrace(this ILogger logger, Exception? exception, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Trace, exception, message, args);

        public static void LogDebug(this ILogger logger, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Debug, null, message, args);

        public static void LogDebug(this ILogger logger, Exception? exception, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Debug, exception, message, args);

        public static void LogInformation(this ILogger logger, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Information, null, message, args);

        public static void LogInformation(this ILogger logger, Exception? exception, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Information, exception, message, args);

        public static void LogWarning(this ILogger logger, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Warning, null, message, args);

        public static void LogWarning(this ILogger logger, Exception? exception, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Warning, exception, message, args);

        public static void LogError(this ILogger logger, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Error, null, message, args);

        public static void LogError(this ILogger logger, Exception? exception, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Error, exception, message, args);

        public static void LogCritical(this ILogger logger, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Critical, null, message, args);

        public static void LogCritical(this ILogger logger, Exception? exception, LocalizedString message, params object?[] args) =>
            Write(logger, LogLevel.Critical, exception, message, args);

        // A localizer mock that was never set up hands over null; it is logged without an id, as the string overloads did.
        private static void Write(ILogger logger, LogLevel level, Exception? exception, LocalizedString? message, object?[] args) =>
            logger.Log(level, message is null ? default : LogEventIds.For(message.Name), exception, message?.Value, args);
    }

    public static class LogEventIds
    {
        // The same key gives the same id in every language and every build: an FNV-1a hash of its name,
        // so adding or reordering keys never moves one.
        public static EventId For(string key)
        {
            var hash = 2166136261u;
            foreach (var character in key)
            {
                hash = (hash ^ character) * 16777619u;
            }

            return new EventId((int)(hash & int.MaxValue), key);
        }
    }
}
