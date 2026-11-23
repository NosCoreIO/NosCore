//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MaintenanceService
{
    public sealed class MaintenanceService(IClock clock, ISessionRegistry sessionRegistry, IChannelHub channelHub,
        IPubSubHub pubSubHub) : IMaintenanceService, ISingletonService
    {
        public static readonly Duration WarningTime = Duration.FromMinutes(5);

        private static readonly (Duration Before, Game18NConstString Message, int? Seconds)[] Countdown =
        [
            (Duration.FromSeconds(30), Game18NConstString.ServerWillBeDisonnected, 30),
            (Duration.FromSeconds(25), Game18NConstString.ServerWillBeDisonnected, 25),
            (Duration.FromSeconds(20), Game18NConstString.ServerWillBeDisonnected, 20),
            (Duration.FromSeconds(15), Game18NConstString.ServerWillBeDisonnected, 15),
            (Duration.FromSeconds(10), Game18NConstString.ServerWillBeDisonnected, 10),
            (Duration.FromSeconds(5), Game18NConstString.ThanksForParticipating, null),
        ];

        private readonly Lock _sync = new();
        private Instant? _shutdownAt;
        private bool _warned;
        private int _nextStep;

        public async Task SetMaintenanceAsync(bool isGlobal, bool value, Duration? delay)
        {
            if (value && delay is { } wait && wait > Duration.Zero)
            {
                await ScheduleAsync(isGlobal, clock.GetCurrentInstant() + wait);
                return;
            }

            if (!value)
            {
                await ScheduleAsync(isGlobal, null);
            }

            await channelHub.SetMaintenance(isGlobal, value);
        }

        public void ScheduleShutdown(Instant? shutdownAt)
        {
            lock (_sync)
            {
                _shutdownAt = shutdownAt;
                _warned = false;
                // Steps already past would announce more seconds than are left.
                _nextStep = shutdownAt is { } at
                    ? Countdown.TakeWhile(step => step.Before > at - clock.GetCurrentInstant()).Count()
                    : 0;
            }
        }

        public async Task TickAsync()
        {
            var shutDown = false;
            int? minutesLeft = null;
            (Duration Before, Game18NConstString Message, int? Seconds)? step = null;
            lock (_sync)
            {
                if (_shutdownAt is not { } shutdownAt)
                {
                    return;
                }

                var left = shutdownAt - clock.GetCurrentInstant();
                if (left <= Duration.Zero)
                {
                    _shutdownAt = null;
                    shutDown = true;
                }
                else
                {
                    if (!_warned && left <= WarningTime)
                    {
                        _warned = true;
                        minutesLeft = (int)Math.Ceiling(left.TotalMinutes);
                    }

                    while (_nextStep < Countdown.Length && left <= Countdown[_nextStep].Before)
                    {
                        step = Countdown[_nextStep++];
                    }
                }
            }

            if (minutesLeft is { } minutes)
            {
                await WarnAsync(minutes);
            }

            if (step is { } countdown)
            {
                await sessionRegistry.BroadcastPacketAsync(countdown.Seconds is { } seconds
                    ? new MsgiPacket { Message = countdown.Message, ArgumentType = 4, Game18NArguments = { seconds } }
                    : new MsgiPacket { Message = countdown.Message });
            }

            if (shutDown)
            {
                await ShutDownAsync();
            }
        }

        private Task ScheduleAsync(bool isGlobal, Instant? shutdownAt)
        {
            if (isGlobal)
            {
                return pubSubHub.SendMessageAsync(new MaintenanceScheduleData { ShutdownAt = shutdownAt });
            }

            ScheduleShutdown(shutdownAt);
            return Task.CompletedTask;
        }

        private Task WarnAsync(int minutes) =>
            Task.WhenAll(sessionRegistry.GetSessions().Select(session => session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Shout,
                Message = Game18NConstString.ServerWillDisconnectShortly,
                ArgumentType = 4,
                Game18NArguments = { minutes },
            })));

        // Game masters may enter during maintenance, so they are left connected.
        private async Task ShutDownAsync()
        {
            await channelHub.SetMaintenance(false, true);
            await Task.WhenAll(sessionRegistry.GetAllSessions()
                .Where(s => s.Sender is not ClientSession { HasAccount: true, Account.Authority: >= AuthorityType.GameMaster })
                .Select(s => s.Disconnect()));
        }
    }
}
