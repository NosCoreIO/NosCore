//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    //    VNUM	{NpcMonsterVNum}
    //    NAME {NameI18NKey}

    //    LEVEL	{Level}
    //    RACE	{Race}	{RaceType}
    //    ATTRIB	{Element}	{ElementRate}	{FireResistance}	{WaterResistance}	{LightResistance}	{DarkResistance}
    //    HP/MP	{HP}	{MP}
    //    EXP	{XP}	{JXP}
    //    PREATT	{IsHostile}	{NoticeRange}	{Speed}	{RespawnTime}	400
    //    SETTING	0	0	-1	0	0	0
    //    ETC	8	1	0	0	0	0	0	0
    //    PETINFO	1	10	0	50
    //    EFF	200	0	0
    //    ZSKILL	0	1	3	2	12	0	0
    //    WINFO	0	0	0
    //    WEAPON	16	1	0	0	0	11	-20
    //    AINFO	0	0
    //    ARMOR	{CloseDefence}	{DistanceDefence}	{MagicDefence}	{DefenceDodge}	{DistanceDefenceDodge}
    //    SKILL	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    PARTNER	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    BASIC	0	0	4	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    CARD	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    MODE	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    ITEM	2000	9000	1	16	800	1	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0
    //#========================================================

    public class NpcMonsterParser
    {
        private readonly string _fileNpcId = $"{Path.DirectorySeparatorChar}monster.dat";
        private readonly IDao<BCardDto, short> _bCardDao;
        private readonly IDao<DropDto, short> _dropDao;
        private readonly ILogger _logger;
        private readonly IDao<NpcMonsterDto, short> _npcMonsterDao;
        private readonly IDao<NpcMonsterSkillDto, long> _npcMonsterSkillDao;
        private readonly IDao<SkillDto, short> _skillDao;
        private readonly int[] _basicHp = new int[100];
        private readonly int[] _basicPrimaryMp = new int[100];
        private readonly int[] _basicSecondaryMp = new int[100];
        private Dictionary<short, SkillDto>? _skilldb;
        private Dictionary<short, List<DropDto>>? _dropdb;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public NpcMonsterParser(IDao<SkillDto, short> skillDao, IDao<BCardDto, short> bCardDao,
            IDao<DropDto, short> dropDao, IDao<NpcMonsterSkillDto, long> npcMonsterSkillDao,
            IDao<NpcMonsterDto, short> npcMonsterDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _skillDao = skillDao;
            _bCardDao = bCardDao;
            _dropDao = dropDao;
            _npcMonsterSkillDao = npcMonsterSkillDao;
            _npcMonsterDao = npcMonsterDao;
            _logger = logger;
            _logLanguage = logLanguage;
            InitStats();
        }


        public FluentParserBuilder<NpcMonsterDto> BuildParser(string folder)
        {
            return FluentParserBuilder<NpcMonsterDto>.Create(folder + _fileNpcId, "#========================================================", 1)
                .Field(x => x.NpcMonsterVNum, "VNUM", 0, 2, s => Convert.ToInt16(s), "Monster vnum")
                .Field(x => x.NameI18NKey, "NAME", 0, 2, s => s, "Localization key (zts##e)")
                .Field(x => x.Race, "RACE", 0, 2, s => Convert.ToByte(s), "Race group (plant, animal, monster, dragon, …)")
                .Field(x => x.RaceType, "RACE", 0, 3, s => Convert.ToByte(s), "Subtype within the race group")
                .Field(x => x.Element, "ATTRIB", 0, 2, s => Convert.ToByte(s), "Primary elemental alignment")
                .Field(x => x.ElementRate, "ATTRIB", 0, 3, s => Convert.ToInt16(s), "Element rate %")
                .Field(x => x.FireResistance, "ATTRIB", 0, 4, s => Convert.ToInt16(s), "Fire resistance")
                .Field(x => x.WaterResistance, "ATTRIB", 0, 5, s => Convert.ToInt16(s), "Water resistance")
                .Field(x => x.LightResistance, "ATTRIB", 0, 6, s => Convert.ToInt16(s), "Light resistance")
                .Field(x => x.DarkResistance, "ATTRIB", 0, 7, s => Convert.ToInt16(s), "Dark resistance")
                .Field(x => x.IsHostile, "PREATT", 0, 2, s => s != "0", "Non-zero means the mob aggroes on sight")
                .Field(x => x.NoticeRange, "PREATT", 0, 4, s => Convert.ToByte(s), "Aggro radius in cells")
                .Field(x => x.Speed, "PREATT", 0, 5, s => Convert.ToByte(s), "Movement speed")
                .Field(x => x.RespawnTime, "PREATT", 0, 6, s => Convert.ToInt32(s), "Respawn delay, deciseconds")
                .Field(x => x.BasicSkill, "EFF", 0, 2, s => Convert.ToInt16(s), "Basic on-attack effect id")
                .Field(x => x.AttackClass, "ZSKILL", 0, 2, s => Convert.ToByte(s), "Attack class (melee/ranged/magic)")
                .Field(x => x.BasicRange, "ZSKILL", 0, 3, s => Convert.ToByte(s), "Basic attack range in cells")
                .Field(x => x.BasicArea, "ZSKILL", 0, 5, s => Convert.ToByte(s), "Basic attack area-of-effect radius")
                .Field(x => x.BasicCooldown, "ZSKILL", 0, 6, s => Convert.ToInt16(s), "Basic attack cooldown, deciseconds")
                .Field(x => x.CanWalk, "ETC", 0, 2, s => (Convert.ToInt64(s) & 1) == 0, "ETC bit 0 clear -> can walk")
                .Field(x => x.CanCollect, "ETC", 0, 2, s => (Convert.ToInt64(s) & 2) != 0, "ETC bit 1 -> harvestable")
                .Field(x => x.CantDebuff, "ETC", 0, 2, s => (Convert.ToInt64(s) & 4) != 0, "ETC bit 2 -> immune to debuffs")
                .Field(x => x.CanCatch, "ETC", 0, 2, s => (Convert.ToInt64(s) & 8) != 0, "ETC bit 3 -> catchable (mate)")
                .Field(x => x.DisappearAfterSeconds, "ETC", 0, 2, s => (Convert.ToInt64(s) & 16) != 0, "ETC bit 4 -> despawns on a timer")
                .Field(x => x.DisappearAfterHitting, "ETC", 0, 2, s => (Convert.ToInt64(s) & 32) != 0, "ETC bit 5 -> despawns after a hit")
                .Field(x => x.HasMode, "ETC", 0, 2, s => (Convert.ToInt64(s) & 64) != 0, "ETC bit 6 -> uses a MODE")
                .Field(x => x.DisappearAfterSecondsMana, "ETC", 0, 2, s => (Convert.ToInt64(s) & 128) != 0, "ETC bit 7 -> despawns when mana empties")
                .Field(x => x.OnDefenseOnlyOnce, "ETC", 0, 2, s => (Convert.ToInt64(s) & 256) != 0, "ETC bit 8 -> defensive AI fires once")
                .Field(x => x.HasDash, "ETC", 0, 2, s => (Convert.ToInt64(s) & 512) != 0, "ETC bit 9 -> has a dash skill")
                .Field(x => x.RegenerateHpOverTime, "ETC", 0, 2, s => (Convert.ToInt64(s) & 1024) != 0, "ETC bit 10 -> passive HP regen")
                .Field(x => x.CantVoke, "ETC", 0, 2, s => (Convert.ToInt64(s) & 2048) != 0, "ETC bit 11 -> immune to voke")
                .Field(x => x.DontDrainHpAfterSeconds, "ETC", 0, 2, s => (Convert.ToInt64(s) & 268435456) != 0, "ETC bit 28 -> skips HP drain over time")
                .Field(x => x.CantTargetInfo, "ETC", 0, 2, s => (Convert.ToInt64(s) & 2147483648L) != 0, "ETC bit 31 -> hidden from target-info UI")
                .Field(x => x.AlwaysActive, "MODE", 0, 26, s => s != "0", "Mode always active when non-zero")
                .Field(x => x.Limiter, "MODE", 0, 27, s => Convert.ToByte(s), "Mode limiter")
                .Field(x => x.HpThreshold, "MODE", 0, 28, s => Convert.ToInt16(s), "HP% threshold that swaps the mode / item vnum")
                .Field(x => x.RangeThreshold, "MODE", 0, 29, s => Convert.ToInt16(s), "Range threshold for mode swap")
                .Field(x => x.CModeVNum, "MODE", 0, 30, s => Convert.ToInt16(s), "c_mode vnum (transform target)")
                .Field(x => x.CellMinRange, "MODE", 0, 31, s => Convert.ToByte(s), "Minimum cells before the mode engages")
                .Field(x => x.Midgard, "MODE", 0, 32, s => Convert.ToInt32(s), "Midgard-specific data")
                .Field(x => x.Level, chunk => Level(chunk),
                    reads: new[] { ("LEVEL", 0, 2) },
                    source: "LEVEL[2]", description: "Parsed by Level(chunk) helper")
                .Field(x => x.HeroXp, chunk => ImportXp(chunk) / 25,
                    reads: new[] { ("EXP", 0, 2), ("LEVEL", 0, 2) },
                    source: "EXP[2] / 25", description: "Hero-level XP award, scaled down from XP")
                .Field(x => x.MaxHp, chunk => Convert.ToInt32(chunk["HP/MP"][0][2]) + _basicHp[Level(chunk)],
                    reads: new[] { ("HP/MP", 0, 2), ("LEVEL", 0, 2) },
                    source: "HP/MP[2] + basicHp[Level]", description: "Override HP plus level-based baseline")
                .Field(x => x.MaxMp, chunk => Convert.ToInt32(chunk["HP/MP"][0][3]) + (Convert.ToByte(chunk["RACE"][0][2]) == 0 ? _basicPrimaryMp[Level(chunk)] : _basicSecondaryMp[Level(chunk)]),
                    reads: new[] { ("HP/MP", 0, 3), ("LEVEL", 0, 2), ("RACE", 0, 2) },
                    source: "HP/MP[3] + basic(Primary|Secondary)Mp[Level]", description: "Override MP plus level-based baseline, branch on Race")
                .Field(x => x.Xp, chunk => ImportXp(chunk),
                    reads: new[] { ("EXP", 0, 2), ("LEVEL", 0, 2) },
                    source: "EXP[2]", description: "Base XP award")
                .Field(x => x.JobXp, chunk => ImportJxp(chunk),
                    reads: new[] { ("EXP", 0, 3), ("LEVEL", 0, 2) },
                    source: "EXP[3]", description: "Base job XP award")
                .Field(x => x.CloseDefence, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 2 + 18),
                    reads: new[] { ("ARMOR", 0, 2) },
                    source: "ARMOR[2] formula", description: "(armorLvl-1)*2 + 18")
                .Field(x => x.DistanceDefence, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 3 + 17),
                    reads: new[] { ("ARMOR", 0, 2) },
                    source: "ARMOR[2] formula", description: "(armorLvl-1)*3 + 17")
                .Field(x => x.MagicDefence, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 2 + 13),
                    reads: new[] { ("ARMOR", 0, 2) },
                    source: "ARMOR[2] formula", description: "(armorLvl-1)*2 + 13")
                .Field(x => x.DefenceDodge, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 5 + 31),
                    reads: new[] { ("ARMOR", 0, 2) },
                    source: "ARMOR[2] formula", description: "(armorLvl-1)*5 + 31")
                .Field(x => x.DistanceDefenceDodge, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 5 + 31),
                    reads: new[] { ("ARMOR", 0, 2) },
                    source: "ARMOR[2] formula", description: "(armorLvl-1)*5 + 31")
                .Field(x => x.AttackUpgrade, chunk => Convert.ToByte(LoadUnknownData(chunk) == 1 ? chunk["WINFO"][0][2] : chunk["WINFO"][0][4]),
                    reads: new[] { ("WINFO", 0, 2), ("WINFO", 0, 4), ("ETC", 0, 2) },
                    source: "WINFO[2] or WINFO[4] (gated by LoadUnknownData)", description: "Weapon upgrade level")
                .Field(x => x.DefenceUpgrade, chunk => Convert.ToByte(LoadUnknownData(chunk) == 1 ? chunk["WINFO"][0][2] : chunk["AINFO"][0][3]),
                    reads: new[] { ("WINFO", 0, 2), ("AINFO", 0, 3), ("ETC", 0, 2) },
                    source: "WINFO[2] or AINFO[3] (gated by LoadUnknownData)", description: "Armor upgrade level")
                .Field(x => x.VNumRequired, chunk => Convert.ToInt16(chunk["SETTING"][0][4] != "0" && ShouldLoadPetinfo(chunk) ? chunk["PETINFO"][0][2] : chunk["SETTING"][0][4]),
                    reads: new[] { ("SETTING", 0, 4), ("PETINFO", 0, 2), ("ETC", 0, 2) },
                    source: "SETTING[4] or PETINFO[2]", description: "VNum of the item required to tame/interact")
                .Field(x => x.AmountRequired, chunk => Convert.ToByte(chunk["SETTING"][0][4] == "0" ? "1" : ShouldLoadPetinfo(chunk) ? chunk["PETINFO"][0][3] : "0"),
                    reads: new[] { ("SETTING", 0, 4), ("PETINFO", 0, 3), ("ETC", 0, 2) },
                    source: "SETTING[4] or PETINFO[3]", description: "Amount of the required item")
                .Field(x => x.DamageMinimum, chunk => ImportDamageMinimum(chunk),
                    reads: new[] { ("WEAPON", 0, 2), ("WEAPON", 0, 3), ("WEAPON", 0, 4), ("LEVEL", 0, 2) },
                    source: "WEAPON[2..4]", description: "Min attack damage")
                .Field(x => x.DamageMaximum, chunk => ImportDamageMaximum(chunk),
                    reads: new[] { ("WEAPON", 0, 2), ("WEAPON", 0, 3), ("WEAPON", 0, 4), ("WEAPON", 0, 5), ("LEVEL", 0, 2) },
                    source: "WEAPON[2..5]", description: "Max attack damage")
                .Field(x => x.Concentrate, chunk => ImportConcentrate(chunk),
                    reads: new[] { ("WEAPON", 0, 2), ("WEAPON", 0, 3), ("WEAPON", 0, 6) },
                    source: "WEAPON[2,3,6]", description: "Hit rate")
                .Field(x => x.CriticalChance, chunk => ImportCriticalChance(chunk),
                    reads: new[] { ("WEAPON", 0, 3), ("WEAPON", 0, 7) },
                    source: "WEAPON[3,7]", description: "Critical hit chance")
                .Field(x => x.CriticalRate, chunk => ImportCriticalRate(chunk),
                    reads: new[] { ("WEAPON", 0, 3), ("WEAPON", 0, 8) },
                    source: "WEAPON[3,8]", description: "Critical hit damage multiplier")
                .Field(x => x.NpcMonsterSkill, chunk => ImportNpcMonsterSkill(chunk),
                    reads: Enumerable.Range(2, 15).Select(c => ("SKILL", 0, c)).Append(("VNUM", 0, 2)).ToArray(),
                    source: "VNUM[2] + SKILL[2..16]", description: "Up to 5 NpcMonsterSkill entries (vnum/chance/force triples)")
                .Field(x => x.BCards, chunk => ImportBCards(chunk),
                    reads: Enumerable.Range(2, 50).Select(c => ("BASIC", 0, c))
                        .Concat(Enumerable.Range(2, 20).Select(c => ("CARD", 0, c)))
                        .Append(("VNUM", 0, 2)).ToArray(),
                    source: "VNUM[2] + BASIC[2..51] + CARD[2..21]", description: "BCards from BASIC (10 groups of 5) and CARD (4 groups of 5)")
                .Field(x => x.Drop, chunk => ImportDrops(chunk),
                    reads: Enumerable.Range(2, 60).Select(c => ("ITEM", 0, c)).Append(("VNUM", 0, 2)).ToArray(),
                    source: "VNUM[2] + ITEM[2..61] + merged DropDao catalog", description: "20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO")
                .Field(x => x.MonsterType, chunk => ImportMonsterType(chunk),
                    reads: new[] { ("VNUM", 0, 2), ("ETC", 0, 2), ("RACE", 0, 2), ("RACE", 0, 3) },
                    source: "VNUM[2] + ETC[2] + RACE[2,3]", description: "Categorisation (Mate/Normal/Trap/Unknown)")
                .Field(x => x.NoAggresiveIcon, chunk =>
                {
                    var unknowndata = LoadUnknownData(chunk);
                    return (unknowndata == -2147483616 || unknowndata == -2147483647 || unknowndata == -2147483646)
                        && (Convert.ToByte(chunk["RACE"][0][2]) == 8) && (Convert.ToByte(chunk["RACE"][0][3]) == 0);
                }, reads: new[] { ("ETC", 0, 2), ("RACE", 0, 2), ("RACE", 0, 3) },
                source: "ETC[2] + RACE[2,3]", description: "Talkable / trap entities: suppress the aggressive icon")
                .Describe("ETC", "32-bit flag word stored as a decimal integer. Each bit flips a named gameplay behaviour; only a subset is documented in NSgtd.")
                .Describe("MODE", "4 BCard groups of 5 + 7 mode-meta columns; AlwaysActive onwards is meta.")
                .Doc("LEVEL", 2, "monsterLevel", "Base monster level.")
                .Doc("HP/MP", 2, "monsterMaxHpBonus", "Additive HP bonus on top of the level-derived baseline.")
                .Doc("HP/MP", 3, "monsterMaxMpBonus", "Additive MP bonus on top of the level-derived baseline.")
                .Doc("EXP", 2, "monsterXpBonus", "XP awarded on kill.")
                .Doc("EXP", 3, "monsterJobXpBonus", "Job XP awarded on kill.")
                .Doc("SETTING", 2, "iconID", "Target-info avatar index (NSip).")
                .Doc("SETTING", 3, "spawnMobOrColor", "When hostility>20000, the vnum of the mob this NPC spawns; otherwise a color.")
                .Doc("SETTING", 4, "amountOrItem", "Required item vnum (tame/interact) or spawn count.")
                .Doc("SETTING", 5, "spriteSize", "Sprite scale.")
                .Doc("SETTING", 6, "cellSize", "Collision cell footprint.")
                .Doc("SETTING", 7, "unknown0", "Always 0.")
                .Doc("ETC", 3, "unknown1")
                .Doc("ETC", 4, "isPercentileDmg", "Damage output is expressed as a % of the target's max HP.")
                .Doc("ETC", 5, "canOnlyBeDmgedByJajamaruLastSkill", "Only damageable by the final Jajamaru skill.")
                .Doc("ETC", 6, "unknown2", "Always 0.")
                .Doc("ETC", 7, "visibleOnMinimapAsGreenDot")
                .Doc("ETC", 8, "unknown3")
                .Doc("ETC", 9, "isValhallaPartner")
                .Doc("PETINFO", 2, "petInfoVal1", "Role unclear; first PETINFO column.")
                .Doc("PETINFO", 3, "petInfoVal2")
                .Doc("PETINFO", 4, "petInfoVal3")
                .Doc("PETINFO", 5, "petInfoVal4")
                .Doc("EFF", 3, "effIdConstantly", "Constantly-running aura effect id.")
                .Doc("EFF", 4, "effIdOnDeath", "Effect id played on death.")
                .Doc("ZSKILL", 4, "hitChance", "Basic-attack hit chance (unused by the parser).")
                .Doc("ZSKILL", 7, "dashSpeed", "Speed used when dashing.")
                .Doc("ZSKILL", 8, "unknown4")
                .Doc("WINFO", 2, "winfoAttType", "Attack-type override for ARMOR/WEAPON derivation (1 => special path).")
                .Doc("WINFO", 3, "unknown5")
                .Doc("WINFO", 4, "weaponGrade", "Weapon grade; used as AttackUpgrade when WINFO[0]!=1.")
                .Doc("WEAPON", 2, "weaponLvl")
                .Doc("WEAPON", 3, "weaponRange")
                .Doc("WEAPON", 4, "weaponDmgMin")
                .Doc("WEAPON", 5, "weaponDmgMax")
                .Doc("WEAPON", 6, "weaponHitRate")
                .Doc("WEAPON", 7, "weaponCritChance")
                .Doc("WEAPON", 8, "weaponCritDmg")
                .Doc("AINFO", 2, "ainfoDefType", "Defence-type override for ARMOR derivation.")
                .Doc("AINFO", 3, "armorGrade", "Armor grade; used as DefenceUpgrade when WINFO[0]!=1.")
                .Doc("ARMOR", 2, "armorLvl", "Armor level — all Close/Distance/Magic defence + dodge are derived from this.")
                .Doc("ARMOR", 3, "meleeDef", "Stored in .dat but we recompute from armorLvl.")
                .Doc("ARMOR", 4, "rangedDef", "Stored in .dat but we recompute from armorLvl.")
                .Doc("ARMOR", 5, "magicDef", "Stored in .dat but we recompute from armorLvl.")
                .Doc("ARMOR", 6, "dodge", "Stored in .dat but we recompute from armorLvl.")
                .Doc("SKILL", 2, "skill1Vnum", "First skill vnum.")
                .Doc("SKILL", 3, "skill1Chance", "Cast chance % for skill 1.")
                .Doc("SKILL", 4, "skill1Force", "Priority force for skill 1; groups 2-5 repeat at cols 5/8/11/14.")
                .Doc("PARTNER", 2, "partnerUnused", "PARTNER block is always 20 zeros in vanilla; unused.")
                .Doc("BASIC", 2, "basicBCard1Vnum", "First BASIC BCard vnum; 10 groups of 5 (cols 2..51) repeat the vnum/val1/val2/sub/target pattern.")
                .Doc("CARD", 2, "cardBCard1Vnum", "First CARD BCard vnum; 4 groups of 5 (cols 2..21) repeat the pattern. CARD slot 2 = death-trigger BCards.")
                .Doc("ITEM", 2, "drop1Vnum", "First drop vnum; 20 groups of 3 (cols 2..61) repeat the vnum/chance/amount triple.");
        }

        public async Task InsertNpcMonstersAsync(string folder)
        {
            _skilldb = _skillDao.LoadAll().ToDictionary(x => x.SkillVNum, x => x);
            _dropdb = _dropDao.LoadAll().Where(x => x.MonsterVNum != null).GroupBy(x => x.MonsterVNum).ToDictionary(x => x.Key ?? 0, x => x.ToList());
            var parser = BuildParser(folder).Build(_logger, _logLanguage);
            var monsters = (await parser.GetDtosAsync()).GroupBy(p => p.NpcMonsterVNum).Select(g => g.First()).ToList();
            await _npcMonsterDao.TryInsertOrUpdateAsync(monsters);
            await _bCardDao.TryInsertOrUpdateAsync(monsters.Where(s => s.BCards != null).SelectMany(s => s.BCards));
            await _dropDao.TryInsertOrUpdateAsync(monsters.Where(s => s.Drop != null).SelectMany(s => s.Drop));
            await _npcMonsterSkillDao.TryInsertOrUpdateAsync(monsters.Where(s => s.NpcMonsterSkill != null).SelectMany(s => s.NpcMonsterSkill));
            _logger.Information(_logLanguage[LogLanguageKey.NPCMONSTERS_PARSED], monsters.Count);
        }

        private int ImportJxp(Dictionary<string, string[][]> chunk)
        {
            var value = Convert.ToInt32(chunk["EXP"][0][3]);
            if (value < 1)
            {
                value *= -1;
            }

            if (Level(chunk) < 61)
            {
                return value + 120;
            }

            return value + 105;
        }

        private int ImportXp(Dictionary<string, string[][]> chunk)
        {
            var value = Convert.ToInt32(chunk["EXP"][0][2]);
            if (value < 1)
            {
                value *= -1;
            }

            if (Level(chunk) >= 19)
            {
                return Math.Abs((Level(chunk) * 60) + Level(chunk) * 10 + value);
            }

            return Math.Abs((Level(chunk) * 60) + value);
        }

        private short ImportDamageMinimum(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16((Convert.ToInt16(chunk["WEAPON"][0][2]) - 1) * 4 + 32 + Convert.ToInt16(chunk["WEAPON"][0][4]) + Math.Round(Convert.ToDecimal((Level(chunk) - 1) / 5))),
                "2" => Convert.ToInt16(Convert.ToInt16(chunk["WEAPON"][0][2]) * 6.5f + 23 + Convert.ToInt16(chunk["WEAPON"][0][4])),
                _ => (short)0,
            };
        }

        private short ImportDamageMaximum(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16((Convert.ToInt16(chunk["WEAPON"][0][2]) - 1) * 6 + 40 + Convert.ToInt16(chunk["WEAPON"][0][5]) - Math.Round(Convert.ToDecimal((Level(chunk) - 1) / 5))),
                "2" => Convert.ToInt16(Convert.ToInt16(chunk["WEAPON"][0][2]) * 6.5f + 23 + Convert.ToInt16(chunk["WEAPON"][0][4])),
                _ => (short)0,
            };

        }

        private short ImportConcentrate(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16((Convert.ToInt16(chunk["WEAPON"][0][2]) - 1) * 5 + 27 + Convert.ToInt16(chunk["WEAPON"][0][6])),
                "2" => Convert.ToInt16(70 + Convert.ToInt16(chunk["WEAPON"][0][6])),
                _ => (short)0,
            };
        }

        private byte ImportCriticalChance(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToByte(4 + Convert.ToInt16(chunk["WEAPON"][0][7])),
                _ => (byte)0,
            };
        }

        private short ImportCriticalRate(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16(70 + Convert.ToInt16(chunk["WEAPON"][0][8])),
                _ => (short)0,
            };
        }

        private List<NpcMonsterSkillDto> ImportNpcMonsterSkill(Dictionary<string, string[][]> chunk)
        {
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);
            var skills = new List<NpcMonsterSkillDto>();
            for (var i = 2; i < chunk["SKILL"][0].Length - 3; i += 3)
            {
                var vnum = short.Parse(chunk["SKILL"][0][i]);
                if ((vnum == -1) || (vnum == 0))
                {
                    break;
                }

                if (_skilldb?.ContainsKey(vnum) == false)
                {
                    continue;
                }

                skills.Add(new NpcMonsterSkillDto
                {
                    SkillVNum = vnum,
                    Rate = Convert.ToInt16(chunk["SKILL"][0][i + 1]),
                    Force = Convert.ToByte(chunk["SKILL"][0][i + 2]),
                    NpcMonsterVNum = monstervnum
                });
            }

            return skills;
        }


        private List<BCardDto> ImportBCards(Dictionary<string, string[][]> chunk)
        {
            var monstercards = new List<BCardDto>();
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);

            for (var i = 0; i < 4; i++)
            {
                var type = (byte)int.Parse(chunk["CARD"][0][5 * i + 2]);
                if ((type == 0) || (type == 255))
                {
                    continue;
                }

                var first = int.Parse(chunk["CARD"][0][5 * i + 3]);
                var itemCard = new BCardDto
                {
                    NpcMonsterVNum = monstervnum,
                    Type = type,
                    SubType = (byte)(int.Parse(chunk["CARD"][0][5 * i + 5]) + 1 * 10 + 1
                        + (first > 0 ? 0 : 1)),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = (uint)(first > 0 ? first : -first) % 4 == 2,
                    FirstData = (short)((first > 0 ? first : -first) / 4),
                    SecondData = (short)(int.Parse(chunk["CARD"][0][5 * i + 4]) / 4),
                    ThirdData = (short)(int.Parse(chunk["CARD"][0][5 * i + 6]) / 4),
                    Slot = (byte)(i + 1)
                };
                monstercards.Add(itemCard);

                first = int.Parse(chunk["BASIC"][0][5 * i + 5]);
                itemCard = new BCardDto
                {
                    NpcMonsterVNum = monstervnum,
                    Type = type,
                    SubType =
                        (byte)((int.Parse(chunk["BASIC"][0][5 * i + 6]) + 1) * 10 + 1 + (first > 0 ? 0 : 1)),
                    FirstData = (short)((first > 0 ? first : -first) / 4),
                    SecondData = (short)(int.Parse(chunk["BASIC"][0][5 * i + 4]) / 4),
                    ThirdData = (short)(int.Parse(chunk["BASIC"][0][5 * i + 3]) / 4),
                    CastType = 1,
                    IsLevelScaled = false,
                    IsLevelDivided = false
                };
                monstercards.Add(itemCard);
            }

            return monstercards;
        }

        private List<DropDto> ImportDrops(Dictionary<string, string[][]> chunk)
        {
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);
            var drops = new List<DropDto>();

            for (var i = 2; i < chunk["ITEM"][0].Length - 3; i += 3)
            {
                var vnum = Convert.ToInt16(chunk["ITEM"][0][i]);
                if (vnum == -1)
                {
                    break;
                }

                if (_dropdb?.ContainsKey(monstervnum) == true && (_dropdb[monstervnum].Count(s => s.VNum == vnum) != 0))
                {
                    continue;
                }

                drops.Add(new DropDto
                {
                    VNum = vnum,
                    Amount = Convert.ToInt32(chunk["ITEM"][0][i + 2]),
                    MonsterVNum = monstervnum,
                    DropChance = Convert.ToInt32(chunk["ITEM"][0][i + 1])
                });
            }

            return drops;
        }

        private MonsterType ImportMonsterType(Dictionary<string, string[][]> chunk)
        {
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);
            var unknownData = LoadUnknownData(chunk);
            if (monstervnum >= 588 && monstervnum <= 607)
            {
                return MonsterType.Elite;
            }

            if (unknownData == -2147481593)
            {
                return MonsterType.Special;
            }

            return MonsterType.Unknown;
        }

        private void InitStats()
        {
            // basicHPLoad
            var baseHp = 138;
            var hPbasup = 18;
            for (var i = 0; i < 100; i++)
            {
                _basicHp[i] = baseHp;
                hPbasup++;
                baseHp += hPbasup;

                if (i == 37)
                {
                    baseHp = 1765;
                    hPbasup = 65;
                }

                if (i < 41)
                {
                    continue;
                }

                if ((99 - i) % 8 == 0)
                {
                    hPbasup++;
                }
            }

            //Race == 0
            _basicPrimaryMp[0] = 10;
            _basicPrimaryMp[1] = 10;
            _basicPrimaryMp[2] = 15;

            var primaryBasup = 5;
            byte count = 0;
            var isStable = true;
            var isDouble = false;

            for (uint i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    _basicPrimaryMp[i] += _basicPrimaryMp[i - 1] + primaryBasup * 2;
                    continue;
                }

                if (!isStable)
                {
                    primaryBasup++;
                    count++;

                    if (count == 2)
                    {
                        if (isDouble)
                        {
                            isDouble = false;
                        }
                        else
                        {
                            isStable = true;
                            isDouble = true;
                            count = 0;
                        }
                    }

                    if (count == 4)
                    {
                        isStable = true;
                        count = 0;
                    }
                }
                else
                {
                    count++;
                    if (count == 2)
                    {
                        isStable = false;
                        count = 0;
                    }
                }

                _basicPrimaryMp[i] = _basicPrimaryMp[i - (i % 10 == 2 ? 2 : 1)] + primaryBasup;
            }

            // Race == 2
            _basicSecondaryMp[0] = 60;
            _basicSecondaryMp[1] = 60;
            _basicSecondaryMp[2] = 78;

            var secondaryBasup = 18;
            var boostup = false;

            for (uint i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    _basicSecondaryMp[i] += _basicSecondaryMp[i - 1] + (int)i + 10;
                    continue;
                }

                boostup = !boostup;
                secondaryBasup += boostup ? 3 : 1;
                _basicSecondaryMp[i] = _basicSecondaryMp[i - (i % 10 == 2 ? 2 : 1)] + secondaryBasup;
            }
        }

        private byte Level(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(chunk["LEVEL"][0][2]);
        }

        private long LoadUnknownData(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt64(chunk["ETC"][0][2]);
        }

        private bool ShouldLoadPetinfo(Dictionary<string, string[][]> chunk)
        {
            var unknownData = LoadUnknownData(chunk);
            return !((unknownData != -2147481593) && (unknownData != -2147481599) && (unknownData != -1610610681));
        }

    }
}
