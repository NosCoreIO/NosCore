//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

// Immutable lookup tables built once at construction. NosCore.Data keeps the
// navigation collections internal, so bootstrap pulls the rows out of their DAOs and
// hands us flat lists which we bucket by foreign-key vnum.
public sealed class NpcCombatCatalog : INpcCombatCatalog, ISingletonService
{
    private static readonly IReadOnlyList<NpcMonsterSkillDto> EmptyNpcSkills = Array.Empty<NpcMonsterSkillDto>();
    private static readonly IReadOnlyList<DropDto> EmptyDrops = Array.Empty<DropDto>();
    private static readonly IReadOnlyList<BCardDto> EmptyBCards = Array.Empty<BCardDto>();

    private readonly IReadOnlyDictionary<short, IReadOnlyList<NpcMonsterSkillDto>> _skillsByMob;
    private readonly IReadOnlyDictionary<short, IReadOnlyList<DropDto>> _dropsByMob;
    private readonly IReadOnlyDictionary<short, IReadOnlyList<BCardDto>> _bcardsByMob;
    private readonly IReadOnlyDictionary<short, IReadOnlyList<BCardDto>> _deathBCardsByMob;
    private readonly IReadOnlyDictionary<short, IReadOnlyList<BCardDto>> _bcardsBySkill;

    public NpcCombatCatalog(
        List<NpcMonsterSkillDto> npcMonsterSkills,
        List<DropDto> drops,
        List<BCardDto> bCards)
    {
        _skillsByMob = npcMonsterSkills
            .GroupBy(s => s.NpcMonsterVNum)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<NpcMonsterSkillDto>)g.ToArray());

        _dropsByMob = drops
            .Where(d => d.MonsterVNum.HasValue)
            .GroupBy(d => d.MonsterVNum!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DropDto>)g.ToArray());

        _bcardsByMob = bCards
            .Where(b => b.NpcMonsterVNum.HasValue)
            .GroupBy(b => b.NpcMonsterVNum!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BCardDto>)g.ToArray());

        _deathBCardsByMob = bCards
            .Where(b => b.NpcMonsterVNum.HasValue && b.Slot == 2)
            .GroupBy(b => b.NpcMonsterVNum!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BCardDto>)g.ToArray());

        _bcardsBySkill = bCards
            .Where(b => b.SkillVNum.HasValue)
            .GroupBy(b => b.SkillVNum!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BCardDto>)g.ToArray());
    }

    public IReadOnlyList<NpcMonsterSkillDto> GetSkills(short npcMonsterVnum)
        => _skillsByMob.TryGetValue(npcMonsterVnum, out var list) ? list : EmptyNpcSkills;

    public IReadOnlyList<DropDto> GetDrops(short npcMonsterVnum)
        => _dropsByMob.TryGetValue(npcMonsterVnum, out var list) ? list : EmptyDrops;

    public IReadOnlyList<BCardDto> GetNpcBCards(short npcMonsterVnum)
        => _bcardsByMob.TryGetValue(npcMonsterVnum, out var list) ? list : EmptyBCards;

    public IReadOnlyList<BCardDto> GetDeathBCards(short npcMonsterVnum)
        => _deathBCardsByMob.TryGetValue(npcMonsterVnum, out var list) ? list : EmptyBCards;

    public IReadOnlyList<BCardDto> GetSkillBCards(short skillVnum)
        => _bcardsBySkill.TryGetValue(skillVnum, out var list) ? list : EmptyBCards;
}
