//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.Dto;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.ShellOptionService;

public interface IShellOptionService
{
    IReadOnlyList<EquipmentOptionDto> Load(Guid wearableInstanceId);

    // Empty when the shell has no kind or rarity a shell table covers.
    IReadOnlyList<EquipmentOptionDto> Roll(WearableInstance shell);

    Task SaveAsync(Guid wearableInstanceId, IEnumerable<EquipmentOptionDto> options);

    Task ClearAsync(Guid wearableInstanceId);

    // Options belong to one instance id: an item handed over under a new id needs its own copy.
    // False when the copy could not be written.
    Task<bool> CopyAsync(Guid fromWearableInstanceId, Guid toWearableInstanceId);
}
