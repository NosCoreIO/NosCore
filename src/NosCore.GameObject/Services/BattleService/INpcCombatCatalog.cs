//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService;

// The NpcMonsterDto doesn't expose its NpcMonsterSkill / Drop / BCards collections
// publicly (they're internal to NosCore.Data). This catalog indexes them by vnum at
// startup so combat code can reach them without bumping up accessibility or hitting
// the database on every hit.
public interface INpcCombatCatalog
{
    IReadOnlyList<NpcMonsterSkillDto> GetSkills(short npcMonsterVnum);

    IReadOnlyList<DropDto> GetDrops(short npcMonsterVnum);

    IReadOnlyList<BCardDto> GetNpcBCards(short npcMonsterVnum);

    IReadOnlyList<BCardDto> GetSkillBCards(short skillVnum);
}
