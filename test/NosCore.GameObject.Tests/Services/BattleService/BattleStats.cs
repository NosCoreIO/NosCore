//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Tests.Shared.AutoFixture;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    internal static class BattleStats
    {
        public static BattleStatsProvider Provider(IBuffService buffs, IEquipmentStatsService equipment,
            NosCore.GameObject.Services.SkillService.IPassiveSkillService? passives = null,
            INpcCombatCatalog? npcCombatCatalog = null)
        {
            var fixture = new NosCoreFixture();
            fixture.Inject(buffs);
            fixture.Inject(equipment);
            if (passives != null)
            {
                fixture.Inject(passives);
            }

            if (npcCombatCatalog != null)
            {
                fixture.Inject(npcCombatCatalog);
            }

            return fixture.Create<BattleStatsProvider>();
        }
    }
}
