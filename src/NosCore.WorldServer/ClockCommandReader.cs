//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Core.Time;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer
{
    // Runs only on a world started with TestClock: "advance 00:20:00" on standard input moves its clock
    // forward, and the log line that follows tells the caller the jump is done.
    public sealed class ClockCommandReader(AdjustableTimeProvider time, IClock clock, ILogger<ClockCommandReader> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage) : BackgroundService
    {
        private const string Advance = "advance ";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            while (!stoppingToken.IsCancellationRequested && await Console.In.ReadLineAsync(stoppingToken) is { } line)
            {
                if (line.StartsWith(Advance, StringComparison.Ordinal)
                    && TimeSpan.TryParse(line[Advance.Length..], CultureInfo.InvariantCulture, out var by)
                    && by >= TimeSpan.Zero)
                {
                    time.Advance(by);
                    logger.LogInformation(logLanguage[LogLanguageKey.CLOCK_ADVANCED], clock.GetCurrentInstant());
                }
            }
        }
    }
}
