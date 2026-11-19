//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

public sealed class NpcMonsterCatalog(List<NpcMonsterDto> npcMonsters) : INpcMonsterCatalog, ISingletonService
{
    private readonly FrozenDictionary<int, NpcMonsterDto> _byVNum =
        npcMonsters.GroupBy(m => (int)m.NpcMonsterVNum).ToFrozenDictionary(g => g.Key, g => g.First());

    public NpcMonsterDto? GetNpcMonster(int vnum) => _byVNum.GetValueOrDefault(vnum);
}
