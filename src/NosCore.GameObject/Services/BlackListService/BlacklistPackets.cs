//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.Packets.ServerPackets.Relations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BlackListService;

public static class BlacklistPackets
{
    public static async Task<BlinitPacket> GenerateBlinitAsync(this PlayerComponentBundle player, IBlacklistHub blacklistHub)
    {
        var subpackets = new List<BlinitSubPacket?>();
        var blacklist = await blacklistHub.GetBlacklistedAsync(player.CharacterId);
        foreach (var b in blacklist)
        {
            subpackets.Add(new BlinitSubPacket
            {
                RelatedCharacterId = b.CharacterId,
                CharacterName = b.CharacterName
            });
        }
        return new BlinitPacket { SubPackets = subpackets };
    }
}
