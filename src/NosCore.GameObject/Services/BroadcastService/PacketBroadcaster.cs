//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BroadcastService
{
    public class PacketBroadcaster(ISessionRegistry sessionRegistry) : IPacketBroadcaster
    {
        public async Task SendToAsync(IPacketTarget target, IPacket packet)
        {
            await SendToAsync(target, new[] { packet });
        }

        public async Task SendToAsync(IPacketTarget target, IEnumerable<IPacket> packets)
        {
            var packetList = packets.ToList();
            var sessions = GetTargetSessions(target);

            var tasks = sessions.Select(s => s.Sender.SendPacketsAsync(packetList));
            await Task.WhenAll(tasks);
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
