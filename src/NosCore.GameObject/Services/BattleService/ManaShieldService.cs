//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Services.BattleService;

public sealed class ManaShieldService(IBuffService buffService, IEquipmentStatsService equipmentStatsService)
    : IManaShieldService
{
    public async Task<int> AbsorbAsync(IAliveEntity target, int damage)
    {
        if (damage <= 0 || target.Mp <= 0)
        {
            return damage;
        }

        var percent = buffService.GetActiveBuffs(target).SelectMany(b => b.BCards)
            .Concat(equipmentStatsService.Resolve(target).BCards ?? [])
            .Where(c => c.Effect() == BCardEffect.LightAndShadowInflictDamageToMp)
            .Sum(c => BattleStatsProvider.ScaleByLevel(c, target.Level));
        var absorbed = (int)Math.Min((long)damage * Math.Min(percent, 100) / 100, target.Mp);
        if (absorbed <= 0)
        {
            return damage;
        }

        target.Mp -= absorbed;
        if (target is PlayerComponentBundle player)
        {
            await player.SendPacketAsync(player.GenerateStat());
        }

        return damage - absorbed;
    }
}
