//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Ecs.Components;

// The storage windows the server opened for the player; a map change starts them all closed.
public record struct WarehouseWindowComponent(bool WarehouseOpen, bool PartnerBackpackOpen, bool FamilyWarehouseOpen);
