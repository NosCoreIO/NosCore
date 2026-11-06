//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.ItemInfoService;

public interface IItemInfoService
{
    // The detail window the client opens on a right click: the item's stats and options.
    // Null for the items it does not cover yet.
    IPacket? GenerateInfo(IItemInstance itemInstance);
}
