//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.InventoryService
{
    public static class PocketTypeExtensions
    {
        public static NoscorePocketType ToNoscorePocketType(this PocketType type) =>
            type == PocketType.Wear ? NoscorePocketType.Wear : (NoscorePocketType)type;
    }
}
