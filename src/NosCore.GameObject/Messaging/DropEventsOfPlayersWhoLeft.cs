//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Messaging.Events;
using Wolverine;

namespace NosCore.GameObject.Messaging
{
    public static class DropEventsOfPlayersWhoLeft
    {
        public static HandlerContinuation Before(IPlayerEvent message) =>
            message.PlayerHasLeft ? HandlerContinuation.Stop : HandlerContinuation.Continue;
    }
}
