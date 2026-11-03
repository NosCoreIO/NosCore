//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Ecs.Extensions;

public static class DelayExtensions
{
    public static Task StartDelayAsync(this PlayerComponentBundle player, DelayPacket delay, Instant now)
    {
        player.PendingDelayType = delay.Type;
        player.PendingDelayReadyAt = now + Duration.FromMilliseconds(delay.Delay);
        return player.SendPacketAsync(delay);
    }

    // The client sends the bar's follow-up packet itself, so it is only honoured once the bar this
    // server started has run its full length.
    public static bool TryCompleteDelay(this PlayerComponentBundle player, DelayPacketType type, Instant now)
    {
        if (player.PendingDelayType != type || now < player.PendingDelayReadyAt)
        {
            return false;
        }

        player.PendingDelayType = null;
        return true;
    }
}
