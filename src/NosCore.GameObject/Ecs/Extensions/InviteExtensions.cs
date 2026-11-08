//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Ecs.Extensions;

public enum InviteType
{
    Friend,
    Family,
    Miniland
}

public static class InviteExtensions
{
    public static readonly Duration InviteCooldown = Duration.FromSeconds(5);

    // Records the invite when it is allowed; otherwise tells the player how long to wait.
    public static async Task<bool> TryStartInviteAsync(this PlayerComponentBundle player, InviteType type,
        long targetId, IClock clock)
    {
        var now = clock.GetCurrentInstant();
        var key = (type, targetId);
        if (player.LastInvites.TryGetValue(key, out var last))
        {
            var remaining = last + InviteCooldown - now;
            if (remaining > Duration.Zero)
            {
                await player.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.CannotSendInvite,
                    ArgumentType = 4,
                    Game18NArguments = { (long)Math.Ceiling(remaining.TotalSeconds) }
                });
                return false;
            }
        }

        player.LastInvites[key] = now;
        return true;
    }
}
