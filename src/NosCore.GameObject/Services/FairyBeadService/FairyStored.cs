//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.InventoryService;

namespace NosCore.GameObject.Services.FairyBeadService;

public sealed record FairyStored(InventoryItemInstance Fairy, InventoryItemInstance? Remaining, InventoryItemInstance Bead);
