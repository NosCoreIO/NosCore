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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.BroadcastService
{
    public class PacketBroadcaster(ISessionRegistry sessionRegistry) : IPacketBroadcaster
    {
        public async Task SendToAsync(IPacketTarget target, IPacket packet)
        {
            await SendToAsync(target, new[] { packet }).ConfigureAwait(false);
        }

        public async Task SendToAsync(IPacketTarget target, IEnumerable<IPacket> packets)
        {
            var packetList = packets.ToList();
            var sessions = GetTargetSessions(target);

            var tasks = sessions.Select(s => s.Sender.SendPacketsAsync(packetList));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private IEnumerable<SessionInfo> GetTargetSessions(IPacketTarget target)
        {
            return target switch
            {
                SessionTarget sessionTarget =>
                    GetSessionByChannelId(sessionTarget.ChannelId),

                CharacterTarget characterTarget =>
                    GetSessionByCharacterId(characterTarget.CharacterId),

                MapTarget mapTarget =>
                    FilterSessions(sessionRegistry.GetSessionsByMapInstance(mapTarget.MapInstanceId), mapTarget.ExcludeFilter),

                GroupTarget groupTarget =>
                    sessionRegistry.GetSessionsByGroup(groupTarget.GroupId),

                EveryoneTarget everyoneTarget =>
                    FilterSessions(sessionRegistry.GetAllSessions(), everyoneTarget.ExcludeFilter),

                _ => Enumerable.Empty<SessionInfo>()
            };
        }

        private IEnumerable<SessionInfo> GetSessionByChannelId(string channelId)
        {
            var sender = sessionRegistry.GetSenderByChannelId(channelId);
            if (sender != null)
            {
                var session = sessionRegistry.GetAllSessions().FirstOrDefault(s => s.ChannelId == channelId);
                if (session != null)
                {
                    yield return session;
                }
            }
        }

        private IEnumerable<SessionInfo> GetSessionByCharacterId(long characterId)
        {
            var session = sessionRegistry.GetAllSessions().FirstOrDefault(s => s.CharacterId == characterId);
            if (session != null)
            {
                yield return session;
            }
        }

        private static IEnumerable<SessionInfo> FilterSessions(IEnumerable<SessionInfo> sessions, System.Func<long, bool>? excludeFilter)
        {
            if (excludeFilter == null)
            {
                return sessions;
            }

            return sessions.Where(s => !s.CharacterId.HasValue || !excludeFilter(s.CharacterId.Value));
        }
    }
}
