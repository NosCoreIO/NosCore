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
    // Carries a data snapshot (not the live MapItemComponentBundle) because Wolverine
    // fans out to multiple handlers and the first one that calls TryRemoveMapItem
    // destroys the ECS entity. Any later handler still reading via the bundle would
    // hit "EntityIdentityComponent missing" on the implicit cast.
    public sealed record MapItemPickedUpEvent(
        ClientSession ClientSession,
        long VisualId,
        short VNum,
        short Amount,
        IItemInstance? ItemInstance,
        GetPacket Packet);
}
