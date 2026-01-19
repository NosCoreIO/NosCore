//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.EventLoaderService.Handlers
{
    [UsedImplicitly]
    public class RemoveTimeoutStaticBonuses : ITimedEventHandler
    {
        private readonly ISessionRegistry _sessionRegistry;

        public RemoveTimeoutStaticBonuses(IClock clock, ISessionRegistry sessionRegistry)
        {
            _clock = clock;
            _lastRun = _clock.GetCurrentInstant();
            _sessionRegistry = sessionRegistry;
        }

        private Instant _lastRun;
        private readonly IClock _clock;

        public bool Condition(Clock condition) => condition.LastTick.Minus(_lastRun).ToTimeSpan() >= TimeSpan.FromMinutes(5);

        public Task ExecuteAsync(RequestData<Instant> runTime)
        {
            return Task.WhenAll(_sessionRegistry.GetCharacters().Select(session =>
            {
                if (session.StaticBonusList.RemoveAll(s => s.DateEnd != null && s.DateEnd < _clock.GetCurrentInstant()) > 0)
                {
                    return session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.MagicItemExpired
                    });
                }
                _lastRun = runTime.Data;
                return Task.CompletedTask;
            }));
        }
    }
}
