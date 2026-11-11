//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.GameObject.Services.InventoryService;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ItemGrantService;

public sealed record ItemGranted(List<InventoryItemInstance> Items, short Amount);

public union ItemGrantResult(ItemGranted, ItemGrantOutcome);
