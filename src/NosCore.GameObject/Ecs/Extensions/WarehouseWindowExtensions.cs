//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;

namespace NosCore.GameObject.Ecs.Extensions;

public static class WarehouseWindowExtensions
{
    public static bool IsWarehouseOpen(this PlayerComponentBundle player, WarehouseType warehouseType) => warehouseType switch
    {
        WarehouseType.Warehouse => player.WarehouseOpen,
        WarehouseType.PetWarehouse => player.PartnerBackpackOpen,
        // Opened for one family: leaving it or joining another closes it.
        WarehouseType.FamilyWareHouse => player.OpenFamilyWarehouseId is { } familyId && familyId == player.Family?.FamilyId,
        _ => false
    };

    public static void OpenWarehouse(this PlayerComponentBundle player, WarehouseType warehouseType) =>
        SetWarehouseOpen(player, warehouseType, true);

    public static void CloseWarehouse(this PlayerComponentBundle player, WarehouseType warehouseType) =>
        SetWarehouseOpen(player, warehouseType, false);

    public static void CloseWarehouses(this PlayerComponentBundle player)
    {
        player.WarehouseOpen = false;
        player.PartnerBackpackOpen = false;
        player.OpenFamilyWarehouseId = null;
    }

    private static void SetWarehouseOpen(PlayerComponentBundle player, WarehouseType warehouseType, bool open)
    {
        switch (warehouseType)
        {
            case WarehouseType.Warehouse:
                player.WarehouseOpen = open;
                break;
            case WarehouseType.PetWarehouse:
                player.PartnerBackpackOpen = open;
                break;
            case WarehouseType.FamilyWareHouse:
                player.OpenFamilyWarehouseId = open ? player.Family?.FamilyId : null;
                break;
        }
    }
}
