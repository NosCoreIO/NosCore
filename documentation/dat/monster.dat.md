# monster.dat

```
	AINFO	{ainfoDefType}	{armorGrade}
	ARMOR	{armorLvl}	{meleeDef}	{rangedDef}	{magicDef}	{dodge}
	ATTRIB	{Element}	{ElementRate}	{FireResistance}	{WaterResistance}	{LightResistance}	{DarkResistance}
	BASIC	{basicBCard1Vnum}
	CARD	{cardBCard1Vnum}
	EFF	{BasicSkill}	{effIdConstantly}	{effIdOnDeath}
	ETC	{CanWalk}	{unknown1}	{isPercentileDmg}	{canOnlyBeDmgedByJajamaruLastSkill}	{unknown2}	{visibleOnMinimapAsGreenDot}	{unknown3}	{isValhallaPartner}
	EXP	{monsterXpBonus}	{monsterJobXpBonus}
	HP/MP	{monsterMaxHpBonus}	{monsterMaxMpBonus}
	ITEM	{drop1Vnum}
	LEVEL	{monsterLevel}
	MODE	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	{AlwaysActive}	{Limiter}	{HpThreshold}	{RangeThreshold}	{CModeVNum}	{CellMinRange}	{Midgard}
	NAME	{NameI18NKey}
	PARTNER	{partnerUnused}
	PETINFO	{petInfoVal1}	{petInfoVal2}	{petInfoVal3}	{petInfoVal4}
	PREATT	{IsHostile}	0	{NoticeRange}	{Speed}	{RespawnTime}
	RACE	{Race}	{RaceType}
	SETTING	{iconID}	{spawnMobOrColor}	{amountOrItem}	{spriteSize}	{cellSize}	{unknown0}
	SKILL	{skill1Vnum}	{skill1Chance}	{skill1Force}
	VNUM	{NpcMonsterVNum}
	WEAPON	{weaponLvl}	{weaponRange}	{weaponDmgMin}	{weaponDmgMax}	{weaponHitRate}	{weaponCritChance}	{weaponCritDmg}
	WINFO	{winfoAttType}	{unknown5}	{weaponGrade}
	ZSKILL	{AttackClass}	{BasicRange}	{hitChance}	{BasicArea}	{BasicCooldown}	{dashSpeed}	{unknown4}
```

## AINFO
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | ainfoDefType |  | Defence-type override for ARMOR derivation. |
| 3 | NonParsed | armorGrade |  | Armor grade; used as DefenceUpgrade when WINFO[0]!=1. |

## ARMOR
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | armorLvl |  | Armor level — all Close/Distance/Magic defence + dodge are derived from this. |
| 3 | NonParsed | meleeDef |  | Stored in .dat but we recompute from armorLvl. |
| 4 | NonParsed | rangedDef |  | Stored in .dat but we recompute from armorLvl. |
| 5 | NonParsed | magicDef |  | Stored in .dat but we recompute from armorLvl. |
| 6 | NonParsed | dodge |  | Stored in .dat but we recompute from armorLvl. |

## ATTRIB
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | Element | Byte | Primary elemental alignment |
| 3 | Parsed | ElementRate | Int16 | Element rate % |
| 4 | Parsed | FireResistance | Int16 | Fire resistance |
| 5 | Parsed | WaterResistance | Int16 | Water resistance |
| 6 | Parsed | LightResistance | Int16 | Light resistance |
| 7 | Parsed | DarkResistance | Int16 | Dark resistance |

## BASIC
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | basicBCard1Vnum |  | First BASIC BCard vnum; 10 groups of 5 (cols 2..51) repeat the vnum/val1/val2/sub/target pattern. |

## CARD
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | cardBCard1Vnum |  | First CARD BCard vnum; 4 groups of 5 (cols 2..21) repeat the pattern. CARD slot 2 = death-trigger BCards. |

## EFF
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | BasicSkill | Int16 | Basic on-attack effect id |
| 3 | NonParsed | effIdConstantly |  | Constantly-running aura effect id. |
| 4 | NonParsed | effIdOnDeath |  | Effect id played on death. |

## ETC
32-bit flag word stored as a decimal integer. Each bit flips a named gameplay behaviour; only a subset is documented in NSgtd.

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | CanCatch | Boolean | ETC bit 3 -> catchable (mate) |
| 2 | Parsed | CanCollect | Boolean | ETC bit 1 -> harvestable |
| 2 | Parsed | CantDebuff | Boolean | ETC bit 2 -> immune to debuffs |
| 2 | Parsed | CantTargetInfo | Boolean | ETC bit 31 -> hidden from target-info UI |
| 2 | Parsed | CantVoke | Boolean | ETC bit 11 -> immune to voke |
| 2 | Parsed | CanWalk | Boolean | ETC bit 0 clear -> can walk |
| 2 | Parsed | DisappearAfterHitting | Boolean | ETC bit 5 -> despawns after a hit |
| 2 | Parsed | DisappearAfterSeconds | Boolean | ETC bit 4 -> despawns on a timer |
| 2 | Parsed | DisappearAfterSecondsMana | Boolean | ETC bit 7 -> despawns when mana empties |
| 2 | Parsed | DontDrainHpAfterSeconds | Boolean | ETC bit 28 -> skips HP drain over time |
| 2 | Parsed | HasDash | Boolean | ETC bit 9 -> has a dash skill |
| 2 | Parsed | HasMode | Boolean | ETC bit 6 -> uses a MODE |
| 2 | Parsed | OnDefenseOnlyOnce | Boolean | ETC bit 8 -> defensive AI fires once |
| 2 | Parsed | RegenerateHpOverTime | Boolean | ETC bit 10 -> passive HP regen |
| 3 | NonParsed | unknown1 |  |  |
| 4 | NonParsed | isPercentileDmg |  | Damage output is expressed as a % of the target's max HP. |
| 5 | NonParsed | canOnlyBeDmgedByJajamaruLastSkill |  | Only damageable by the final Jajamaru skill. |
| 6 | NonParsed | unknown2 |  | Always 0. |
| 7 | NonParsed | visibleOnMinimapAsGreenDot |  |  |
| 8 | NonParsed | unknown3 |  |  |
| 9 | NonParsed | isValhallaPartner |  |  |

## EXP
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | monsterXpBonus |  | XP awarded on kill. |
| 3 | NonParsed | monsterJobXpBonus |  | Job XP awarded on kill. |

## HP/MP
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | monsterMaxHpBonus |  | Additive HP bonus on top of the level-derived baseline. |
| 3 | NonParsed | monsterMaxMpBonus |  | Additive MP bonus on top of the level-derived baseline. |

## ITEM
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | drop1Vnum |  | First drop vnum; 20 groups of 3 (cols 2..61) repeat the vnum/chance/amount triple. |

## LEVEL
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | monsterLevel |  | Base monster level. |

## MODE
4 BCard groups of 5 + 7 mode-meta columns; AlwaysActive onwards is meta.

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Unknown |  |  |  |
| 3 | Unknown |  |  |  |
| 4 | Unknown |  |  |  |
| 5 | Unknown |  |  |  |
| 6 | Unknown |  |  |  |
| 7 | Unknown |  |  |  |
| 8 | Unknown |  |  |  |
| 9 | Unknown |  |  |  |
| 10 | Unknown |  |  |  |
| 11 | Unknown |  |  |  |
| 12 | Unknown |  |  |  |
| 13 | Unknown |  |  |  |
| 14 | Unknown |  |  |  |
| 15 | Unknown |  |  |  |
| 16 | Unknown |  |  |  |
| 17 | Unknown |  |  |  |
| 18 | Unknown |  |  |  |
| 19 | Unknown |  |  |  |
| 20 | Unknown |  |  |  |
| 21 | Unknown |  |  |  |
| 22 | Unknown |  |  |  |
| 23 | Unknown |  |  |  |
| 24 | Unknown |  |  |  |
| 25 | Unknown |  |  |  |
| 26 | Parsed | AlwaysActive | Boolean | Mode always active when non-zero |
| 27 | Parsed | Limiter | Byte | Mode limiter |
| 28 | Parsed | HpThreshold | Int16 | HP% threshold that swaps the mode / item vnum |
| 29 | Parsed | RangeThreshold | Int16 | Range threshold for mode swap |
| 30 | Parsed | CModeVNum | Int16 | c_mode vnum (transform target) |
| 31 | Parsed | CellMinRange | Byte | Minimum cells before the mode engages |
| 32 | Parsed | Midgard | Int32 | Midgard-specific data |

## NAME
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | NameI18NKey | String | Localization key (zts##e) |

## PARTNER
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | partnerUnused |  | PARTNER block is always 20 zeros in vanilla; unused. |

## PETINFO
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | petInfoVal1 |  | Role unclear; first PETINFO column. |
| 3 | NonParsed | petInfoVal2 |  |  |
| 4 | NonParsed | petInfoVal3 |  |  |
| 5 | NonParsed | petInfoVal4 |  |  |

## PREATT
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | IsHostile | Boolean | Non-zero means the mob aggroes on sight |
| 3 | Unknown |  |  |  |
| 4 | Parsed | NoticeRange | Byte | Aggro radius in cells |
| 5 | Parsed | Speed | Byte | Movement speed |
| 6 | Parsed | RespawnTime | Int32 | Respawn delay, deciseconds |

## RACE
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | Race | Byte | Race group (plant, animal, monster, dragon, …) |
| 3 | Parsed | RaceType | Byte | Subtype within the race group |

## SETTING
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | iconID |  | Target-info avatar index (NSip). |
| 3 | NonParsed | spawnMobOrColor |  | When hostility>20000, the vnum of the mob this NPC spawns; otherwise a color. |
| 4 | NonParsed | amountOrItem |  | Required item vnum (tame/interact) or spawn count. |
| 5 | NonParsed | spriteSize |  | Sprite scale. |
| 6 | NonParsed | cellSize |  | Collision cell footprint. |
| 7 | NonParsed | unknown0 |  | Always 0. |

## SKILL
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | skill1Vnum |  | First skill vnum. |
| 3 | NonParsed | skill1Chance |  | Cast chance % for skill 1. |
| 4 | NonParsed | skill1Force |  | Priority force for skill 1; groups 2-5 repeat at cols 5/8/11/14. |

## VNUM
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | NpcMonsterVNum | Int16 | Monster vnum |

## WEAPON
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | weaponLvl |  |  |
| 3 | NonParsed | weaponRange |  |  |
| 4 | NonParsed | weaponDmgMin |  |  |
| 5 | NonParsed | weaponDmgMax |  |  |
| 6 | NonParsed | weaponHitRate |  |  |
| 7 | NonParsed | weaponCritChance |  |  |
| 8 | NonParsed | weaponCritDmg |  |  |

## WINFO
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | winfoAttType |  | Attack-type override for ARMOR/WEAPON derivation (1 => special path). |
| 3 | NonParsed | unknown5 |  |  |
| 4 | NonParsed | weaponGrade |  | Weapon grade; used as AttackUpgrade when WINFO[0]!=1. |

## ZSKILL
| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | AttackClass | Byte | Attack class (melee/ranged/magic) |
| 3 | Parsed | BasicRange | Byte | Basic attack range in cells |
| 4 | NonParsed | hitChance |  | Basic-attack hit chance (unused by the parser). |
| 5 | Parsed | BasicArea | Byte | Basic attack area-of-effect radius |
| 6 | Parsed | BasicCooldown | Int16 | Basic attack cooldown, deciseconds |
| 7 | NonParsed | dashSpeed |  | Speed used when dashing. |
| 8 | NonParsed | unknown4 |  |  |

## Computed / multi-section fields
| DTO property | Type | Source | Description |
|---|---|---|---|
| AmountRequired | Byte | SETTING[2] or PETINFO[1] | Amount of the required item |
| AttackUpgrade | Byte | WINFO[0] or WINFO[2] (gated by LoadUnknownData) | Weapon upgrade level |
| BCards | ICollection`1 | BASIC + CARD + MODE | BCards from BASIC (10 groups of 5), CARD (4 groups), MODE (5 groups) |
| CloseDefence | Int16 | ARMOR[0] formula | (armorLvl-1)*2 + 18 |
| Concentrate | Int16 | WEAPON[varies] | Hit rate |
| CriticalChance | Byte | WEAPON[varies] | Critical hit chance |
| CriticalRate | Int16 | WEAPON[varies] | Critical hit damage multiplier |
| DamageMaximum | Int16 | WEAPON[varies] | Max attack damage |
| DamageMinimum | Int16 | WEAPON[varies] | Min attack damage |
| DefenceDodge | Int16 | ARMOR[0] formula | (armorLvl-1)*5 + 31 |
| DefenceUpgrade | Byte | WINFO[0] or AINFO[1] (gated by LoadUnknownData) | Armor upgrade level |
| DistanceDefence | Int16 | ARMOR[0] formula | (armorLvl-1)*3 + 17 |
| DistanceDefenceDodge | Int16 | ARMOR[0] formula | (armorLvl-1)*5 + 31 |
| Drop | ICollection`1 | ITEM[varies] + merged DropDao catalog | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| HeroXp | Int32 | EXP[0] / 25 | Hero-level XP award, scaled down from XP |
| JobXp | Int32 | EXP[1] | Base job XP award |
| Level | Byte | LEVEL[0] | Parsed by Level(chunk) helper |
| MagicDefence | Int16 | ARMOR[0] formula | (armorLvl-1)*2 + 13 |
| MaxHp | Int32 | HP/MP[0] + basicHp[Level] | Override HP plus level-based baseline |
| MaxMp | Int32 | HP/MP[1] + basic(Primary|Secondary)Mp[Level] | Override MP plus level-based baseline, branch on Race |
| MonsterType | MonsterType | LoadUnknownData + RACE[0,1] | Categorisation (Mate/Normal/Trap/Unknown) |
| NoAggresiveIcon | Boolean | LoadUnknownData + RACE[0,1] | Talkable / trap entities: suppress the aggressive icon |
| NpcMonsterSkill | ICollection`1 | SKILL[0..14] | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| VNumRequired | Int16 | SETTING[2] or PETINFO[0] | VNum of the item required to tame/interact |
| Xp | Int32 | EXP[0] | Base XP award |

