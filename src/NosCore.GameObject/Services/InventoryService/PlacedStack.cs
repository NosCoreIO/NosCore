//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.InventoryService
{
    // Amount is what the stack held right after the placement.
    public sealed record PlacedStack(InventoryItemInstance Stack, short Added, short Amount);
}
