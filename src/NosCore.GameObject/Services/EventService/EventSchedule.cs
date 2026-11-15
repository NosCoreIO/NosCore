//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.EventService
{
    // Starts each configured event's countdown so the event itself begins at its scheduled time.
    public sealed class EventSchedule(IEventService eventService, IClock clock,
        IOptions<WorldConfiguration> worldConfiguration, ILogger<EventSchedule> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // The Act 4 channel only hosts Glacernon.
            var events = worldConfiguration.Value.Events;
            if (events.Count == 0 || worldConfiguration.Value.Act4Channel)
            {
                return;
            }

            var zone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = clock.GetCurrentInstant().InZone(zone).LocalDateTime;
                var next = events
                    .Select(e => (Event: e, Countdown: Start(e, now, eventService.CountdownLength(e.Type).ToTimeSpan())))
                    .Where(e => e.Countdown != null)
                    .OrderBy(e => e.Countdown)
                    .FirstOrDefault();
                if (next.Countdown is not { } countdown)
                {
                    return;
                }

                var wait = countdown.InZoneLeniently(zone).ToInstant() - clock.GetCurrentInstant();
                if (wait > Duration.Zero)
                {
                    await Task.Delay(wait.ToTimeSpan(), stoppingToken);
                }

                if (!eventService.TryStart(next.Event.Type, true))
                {
                    logger.LogWarning(logLanguage[LogLanguageKey.EVENT_STILL_RUNNING], next.Event.Type);
                }

                // Never fire the same slot twice.
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        // The first countdown start strictly after now.
        public static LocalDateTime? Start(ScheduledEventConfiguration configuration, LocalDateTime now, TimeSpan countdown)
        {
            return StartTimes(configuration, now.Date)
                .Concat(StartTimes(configuration, now.Date.PlusDays(1)))
                .Select(start => start.Minus(Period.FromTicks(countdown.Ticks)))
                .Where(start => start > now)
                .OrderBy(start => start)
                .Cast<LocalDateTime?>()
                .FirstOrDefault();
        }

        private static IEnumerable<LocalDateTime> StartTimes(ScheduledEventConfiguration configuration, LocalDate day)
        {
            foreach (var at in configuration.At)
            {
                yield return day.AtMidnight().PlusTicks(at.Ticks);
            }

            if (configuration.Every is not { } every || every <= TimeSpan.Zero)
            {
                yield break;
            }

            for (var time = configuration.Offset; time < TimeSpan.FromDays(1); time += every)
            {
                yield return day.AtMidnight().PlusTicks(time.Ticks);
            }
        }
    }
}
