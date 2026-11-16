//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.ServerPackets.Groups;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.GroupService;

public static class GroupPackets
{
    // Alone, the official server lists nobody; in a group the players come first. Either way
    // the recipient's own pets that are out follow.
    public static PinitPacket GeneratePinit(this PlayerComponentBundle character)
    {
        var group = character.Group;
        var entries = group is { Count: > 1 }
            ? group.GeneratePinit().PinitSubPackets ?? new List<PinitSubPacket?>()
            : new List<PinitSubPacket?>();
        if (character is PlayerComponentBundle player)
        {
            entries.AddRange(player.Mates.Values
                .Where(s => s.IsTeamMember)
                .Select(s => (PinitSubPacket?)s.GenerateSubPinit(player.AccountLanguage)));
        }

        return new PinitPacket { GroupSize = entries.Count, PinitSubPackets = entries };
    }
}
