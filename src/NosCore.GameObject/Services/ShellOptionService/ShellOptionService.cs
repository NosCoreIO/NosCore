//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Algorithm.ShellService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;

namespace NosCore.GameObject.Services.ShellOptionService;

public sealed class ShellOptionService(
    IShellService shellService,
    IRandomNumberSource random,
    IDao<EquipmentOptionDto, Guid> equipmentOptionDao) : IShellOptionService
{
    private const byte WeaponShell = 0;

    public IReadOnlyList<EquipmentOptionDto> Load(Guid wearableInstanceId) =>
        equipmentOptionDao.Where(o => o.WearableInstanceId == wearableInstanceId)?.OrderBy(o => o.Level).ToList()
        ?? [];

    public IReadOnlyList<EquipmentOptionDto> Roll(WearableInstance shell)
    {
        if (shell.Item.ShellType is not { } type || shell.Rare is < 1 or > byte.MaxValue)
        {
            return [];
        }

        var rolled = new List<EquipmentOptionDto>();
        foreach (var slot in shellService.GetSlots(type, (byte)shell.Rare))
        {
            if (!slot.Guaranteed && random.NextDouble() >= 0.5)
            {
                continue;
            }

            var pool = Pool(shell.Item.ItemSubType == WeaponShell, slot.Level)
                .Where(o => rolled.All(r => r.Type != o.Type))
                .ToList();
            if (pool.Count == 0)
            {
                continue;
            }

            var (optionType, minimum, maximum) = pool[Pick(pool.Count)];
            rolled.Add(new EquipmentOptionDto
            {
                Id = Guid.NewGuid(),
                WearableInstanceId = shell.Id,
                Level = slot.Level,
                Type = optionType,
                Value = shellService.GetValue(minimum + Pick(maximum - minimum + 1), shell.Upgrade),
            });
        }

        return rolled;
    }

    public async Task SaveAsync(Guid wearableInstanceId, IEnumerable<EquipmentOptionDto> options)
    {
        await equipmentOptionDao.TryInsertOrUpdateAsync(options.Select(o => o.CopyFor(wearableInstanceId)).ToList());
    }

    public async Task<bool> CopyAsync(Guid fromWearableInstanceId, Guid toWearableInstanceId)
    {
        var options = Load(fromWearableInstanceId);
        return options.Count == 0
            || await equipmentOptionDao.TryInsertOrUpdateAsync(options.Select(o => o.CopyFor(toWearableInstanceId)).ToList());
    }

    public async Task ClearAsync(Guid wearableInstanceId)
    {
        var ids = Load(wearableInstanceId).Select(o => o.Id).ToList();
        if (ids.Count > 0)
        {
            await equipmentOptionDao.TryDeleteAsync(ids);
        }
    }

    private IEnumerable<(byte Type, int Minimum, int Maximum)> Pool(bool weapon, byte level) => weapon
        ? shellService.GetWeaponOptions(level).Select(o => ((byte)o.Type, o.Minimum, o.Maximum))
        : shellService.GetArmorOptions(level).Select(o => ((byte)o.Type, o.Minimum, o.Maximum));

    private int Pick(int count) => Math.Min((int)(random.NextDouble() * count), count - 1);
}
