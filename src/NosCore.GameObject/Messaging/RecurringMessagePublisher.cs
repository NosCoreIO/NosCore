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

namespace NosCore.GameObject.Messaging
{
<<<<<<< HEAD
    // Hosted service that publishes a fresh instance of TMessage every Interval. Use one registration
    // per recurring job — Wolverine handles the actual handler dispatch, retries, and tracing.
=======
>>>>>>> 400adfdd (Swap recurring-jobs infrastructure from Rx-based Clock to Wolverine)
    public sealed class RecurringMessagePublisher<TMessage>(
        IMessageBus bus,
        ILogger<RecurringMessagePublisher<TMessage>> logger,
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
                    logger.LogError(ex, "Recurring publish of {Message} failed", typeof(TMessage).Name);
                }
            }
        }
    }
}
