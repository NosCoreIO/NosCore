//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Packets.ServerPackets.Relations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FriendService;

public static class FriendPackets
{
    public static async Task<FinitPacket> GenerateFinitAsync(this PlayerComponentBundle player, IFriendHub friendHub, IChannelHub channelHub, IPubSubHub pubSubHub)
    {
        var friends = await friendHub.GetFriendsAsync(player.CharacterId);
        var friendSubPackets = new List<FinitSubPacket?>();
        if (friends.Count > 0)
        {
            var online = (await pubSubHub.GetSubscribersAsync())
                .Select(s => s.ConnectedCharacter?.Id)
                .OfType<long>()
                .ToHashSet();

            friendSubPackets.AddRange(friends.Select(relation => new FinitSubPacket
            {
                CharacterId = relation.CharacterId,
                RelationType = relation.RelationType,
                IsOnline = online.Contains(relation.CharacterId),
                CharacterName = relation.CharacterName
            }));
        }

        return new FinitPacket
        {
            SubPackets = friendSubPackets
        };
    }
}
