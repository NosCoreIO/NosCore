//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using NodaTime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication;

public class ChannelPresenceMonitor(MasterClientList masterClientList, Hubs.ChannelHub.ChannelHub channelHub, IClock clock)
    : BackgroundService
{
    private static readonly Duration Expiry = Duration.FromSeconds(15);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = clock.GetCurrentInstant();
            foreach (var (channelKey, lastSeen) in masterClientList.LastSeen)
            {
                if (now - lastSeen > Expiry)
                {
                    channelHub.RemoveChannel(channelKey);
                }
            }
        }
    }
}
