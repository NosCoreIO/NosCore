//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Data.Dto;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public static class EquipmentOptionExtensions
    {
        public static EquipmentOptionDto CopyFor(this EquipmentOptionDto option, Guid wearableInstanceId) => new()
        {
            Id = Guid.NewGuid(),
            WearableInstanceId = wearableInstanceId,
            Level = option.Level,
            Type = option.Type,
            Value = option.Value,
        };

        // One column holds the option of a cellon, a weapon shell or an armour shell; the item it sits on says which.
        public static CellonType Cellon(this EquipmentOptionDto option) => (CellonType)option.Type;

        public static WeaponShellOptionType WeaponShell(this EquipmentOptionDto option) => (WeaponShellOptionType)option.Type;

        public static ArmorShellOptionType ArmorShell(this EquipmentOptionDto option) => (ArmorShellOptionType)option.Type;
    }
}
