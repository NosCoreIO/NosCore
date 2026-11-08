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
    public static Task StartDelayAsync(this PlayerComponentBundle player, DelayPacket delay, object target, Instant now)
    {
        player.PendingDelayType = delay.Type;
        player.PendingDelayReadyAt = now + Duration.FromMilliseconds(delay.Delay);
        player.PendingDelayTarget = target;
        return player.SendPacketAsync(delay);
    }

    // The client sends the bar's follow-up packet itself, so it is only honoured once the bar this
    // server started, for this same target, has run its full length without being interrupted.
    public static bool TryCompleteDelay(this PlayerComponentBundle player, DelayPacketType type, object target, Instant now)
    {
        if (player.PendingDelayType != type || !Equals(player.PendingDelayTarget, target)
            || now < player.PendingDelayReadyAt)
        {
            return false;
        }

        player.CancelDelay();
        return true;
    }

    // Moving, attacking or being hit breaks a bar in progress.
    public static void CancelDelay(this PlayerComponentBundle player)
    {
        player.PendingDelayType = null;
        player.PendingDelayTarget = null;
    }
}
