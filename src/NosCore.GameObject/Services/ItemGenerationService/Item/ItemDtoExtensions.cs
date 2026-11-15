//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public static class ItemDtoExtensions
    {
        public static bool FitsClass(this ItemDto item, CharacterClassType characterClass) =>
            item.Class == 0 || ((item.Class >> (byte)characterClass) & 1) == 1;

        public static bool FitsGender(this ItemDto item, GenderType gender) =>
            item.Sex == 0 || ((item.Sex >> (byte)gender) & 1) == 1;
    }
}
