//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Data.Dto;

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
    }
}
