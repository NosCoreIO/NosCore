//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.InventoryService;

namespace NosCore.GameObject.Services.SpecialistHolderService;

// On Stored, Card is the slot the card left, Remaining what is left in it, and Holder the filled holder.
public readonly record struct SpecialistStoreResult(SpecialistStoreOutcome Outcome,
    InventoryItemInstance? Card = null, InventoryItemInstance? Remaining = null, InventoryItemInstance? Holder = null);
