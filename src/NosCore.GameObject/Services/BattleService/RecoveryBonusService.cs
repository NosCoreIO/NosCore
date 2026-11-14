//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Services.BattleService;

public sealed class RecoveryBonusService(IBuffService buffService, IEquipmentStatsService equipmentStatsService)
    : IRecoveryBonusService
{
    public int Boost(IAliveEntity user, int amount)
    {
        if (amount <= 0)
        {
            return amount;
        }

        var percent = buffService.GetActiveBuffs(user).SelectMany(b => b.BCards)
            .Concat(equipmentStatsService.Resolve(user).BCards ?? [])
            .Where(c => c.Effect() == BCardEffect.LeonaPassiveSkillIncreaseRecoveryItems)
            .Sum(c => BattleStatsProvider.ScaleByLevel(c, user.Level));
        return (int)((long)amount * (100 + percent) / 100);
    }
}
