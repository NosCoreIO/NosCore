//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Messaging
{
    // Hosted service that publishes a fresh instance of TMessage every Interval. Use one registration
    // per recurring job — Wolverine handles the actual handler dispatch, retries, and tracing.
    public sealed class RecurringMessagePublisher<TMessage>(
        IMessageBus bus,
        ILogger<RecurringMessagePublisher<TMessage>> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        TimeSpan interval) : BackgroundService where TMessage : new()
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await bus.PublishAsync(new TMessage()).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, logLanguage[LogLanguageKey.RECURRING_PUBLISH_FAILED], typeof(TMessage).Name);
                }
            }
        }
    }
}
