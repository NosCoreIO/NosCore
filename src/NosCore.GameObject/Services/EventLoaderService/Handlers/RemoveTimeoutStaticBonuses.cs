//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using JetBrains.Annotations;
using NosCore.GameObject.Networking;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ServerPackets.UI;

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
            return Task.WhenAll(_sessionRegistry.GetPlayers().Select(player =>
            {
                if (player.StaticBonusList.RemoveAll(s => s.DateEnd != null && s.DateEnd < _clock.GetCurrentInstant()) > 0)
                {
                    var sender = _sessionRegistry.GetSenderByCharacterId(player.CharacterId);
                    return sender?.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.MagicItemExpired
                    }) ?? Task.CompletedTask;
                }
                _lastRun = runTime.Data;
                return Task.CompletedTask;
            }));
        }
    }
}
