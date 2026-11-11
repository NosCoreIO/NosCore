//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.InventoryService;

namespace NosCore.GameObject.Services.SpecialistHolderService;

// Card is the slot the card left and Remaining what is left in it.
public sealed record SpecialistStored(InventoryItemInstance Card, InventoryItemInstance? Remaining,
    InventoryItemInstance Holder);

public union SpecialistStoreResult(SpecialistStored, SpecialistStoreOutcome);
