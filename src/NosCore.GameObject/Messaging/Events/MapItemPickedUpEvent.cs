//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Drops;

namespace NosCore.GameObject.Messaging.Events
{
    // Carries a data snapshot (not the live MapItemComponentBundle) because the handler that
    // claims the item with TryRemoveMapItem destroys its ECS entity. Invoked inline under the
    // session's packet lock.
    public sealed record MapItemPickedUpEvent(
        ClientSession ClientSession,
        long VisualId,
        short VNum,
        short Amount,
        IItemInstance? ItemInstance,
        GetPacket Packet) : IPlayerEvent
    {
        public bool PlayerHasLeft => !ClientSession.HasPlayerEntity;
    }
}
