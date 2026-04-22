# monster.dat

```text
	AINFO	{ainfoDefType}	{DefenceUpgrade}
	ARMOR	{CloseDefence}	{meleeDef}	{rangedDef}	{magicDef}	{dodge}
	ATTRIB	{Element}	{ElementRate}	{FireResistance}	{WaterResistance}	{LightResistance}	{DarkResistance}
	BASIC	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}
	CARD	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}	{BCards}
	EFF	{BasicSkill}	{EffectIdConstantly}	{EffectIdOnDeath}
	ETC	{CanWalk}	{unknown1}	{IsPercentileDmg}	{CanOnlyBeDmgedByJajamaruLastSkill}	{unknown2}	{VisibleOnMinimapAsGreenDot}	{unknown3}	{IsValhallaPartner}
	EXP	{HeroXp}	{JobXp}
	HP/MP	{MaxHp}	{MaxMp}
	ITEM	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}	{Drop}
	LEVEL	{Level}
	MODE	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	{AlwaysActive}	{Limiter}	{HpThreshold}	{RangeThreshold}	{CModeVNum}	{CellMinRange}	{Midgard}
	NAME	{NameI18NKey}
	PARTNER	{partnerUnused}
	PETINFO	{PetInfoVal1}	{PetInfoVal2}	{PetInfoVal3}	{PetInfoVal4}
	PREATT	{IsHostile}	{GroupAttack}	{NoticeRange}	{Speed}	{RespawnTime}
	RACE	{Race}	{RaceType}
	SETTING	{IconId}	{SpawnMobOrColor}	{VNumRequired}	{SpriteSize}	{CellSize}	{unknown0}
	SKILL	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}	{NpcMonsterSkill}
	VNUM	{NpcMonsterVNum}
	WEAPON	{DamageMinimum}	{DamageMinimum}	{DamageMinimum}	{DamageMaximum}	{Concentrate}	{CriticalChance}	{CriticalRate}
	WINFO	{AttackUpgrade}	{unknown5}	{AttackUpgrade}
	ZSKILL	{AttackClass}	{BasicRange}	{BasicHitChance}	{BasicArea}	{BasicCooldown}	{DashSpeed}	{unknown4}
```

## AINFO

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | NonParsed | ainfoDefType |  | Defence-type override for ARMOR derivation. |
| 3 | Parsed | DefenceUpgrade | Byte | Armor upgrade level |

## ARMOR

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | CloseDefence | Int16 | (armorLvl-1)*2 + 18 |
| 2 | Parsed | DefenceDodge | Int16 | (armorLvl-1)*5 + 31 |
| 2 | Parsed | DistanceDefence | Int16 | (armorLvl-1)*3 + 17 |
| 2 | Parsed | DistanceDefenceDodge | Int16 | (armorLvl-1)*5 + 31 |
| 2 | Parsed | MagicDefence | Int16 | (armorLvl-1)*2 + 13 |
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
| 2 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 3 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 4 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 5 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 6 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 7 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 8 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 9 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 10 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 11 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 12 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 13 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 14 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 15 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 16 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 17 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 18 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 19 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 20 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 21 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 22 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 23 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 24 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 25 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 26 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 27 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 28 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 29 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 30 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 31 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 32 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 33 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 34 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 35 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 36 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 37 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 38 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 39 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 40 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 41 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 42 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 43 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 44 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 45 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 46 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 47 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 48 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 49 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 50 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 51 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |

## CARD

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 3 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 4 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 5 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 6 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 7 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 8 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 9 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 10 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 11 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 12 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 13 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 14 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 15 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 16 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 17 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 18 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 19 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 20 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 21 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |

## EFF

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | BasicSkill | Int16 | Basic on-attack effect id |
| 3 | Parsed | EffectIdConstantly | Int16 | Constantly-running aura effect id |
| 4 | Parsed | EffectIdOnDeath | Int16 | Effect id played on death |

## ETC

32-bit flag word stored as a decimal integer. Each bit flips a named gameplay behaviour; only a subset is documented in NSgtd.

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | AmountRequired | Byte | Amount of the required item |
| 2 | Parsed | AttackUpgrade | Byte | Weapon upgrade level |
| 2 | Parsed | CanCatch | Boolean | ETC bit 3 -> catchable (mate) |
| 2 | Parsed | CanCollect | Boolean | ETC bit 1 -> harvestable |
| 2 | Parsed | CantDebuff | Boolean | ETC bit 2 -> immune to debuffs |
| 2 | Parsed | CantTargetInfo | Boolean | ETC bit 31 -> hidden from target-info UI |
| 2 | Parsed | CantVoke | Boolean | ETC bit 11 -> immune to voke |
| 2 | Parsed | CanWalk | Boolean | ETC bit 0 clear -> can walk |
| 2 | Parsed | DefenceUpgrade | Byte | Armor upgrade level |
| 2 | Parsed | DisappearAfterHitting | Boolean | ETC bit 5 -> despawns after a hit |
| 2 | Parsed | DisappearAfterSeconds | Boolean | ETC bit 4 -> despawns on a timer |
| 2 | Parsed | DisappearAfterSecondsMana | Boolean | ETC bit 7 -> despawns when mana empties |
| 2 | Parsed | DontDrainHpAfterSeconds | Boolean | ETC bit 28 -> skips HP drain over time |
| 2 | Parsed | HasDash | Boolean | ETC bit 9 -> has a dash skill |
| 2 | Parsed | HasMode | Boolean | ETC bit 6 -> uses a MODE |
| 2 | Parsed | MonsterType | MonsterType | Categorisation (Mate/Normal/Trap/Unknown) |
| 2 | Parsed | NoAggresiveIcon | Boolean | Talkable / trap entities: suppress the aggressive icon |
| 2 | Parsed | OnDefenseOnlyOnce | Boolean | ETC bit 8 -> defensive AI fires once |
| 2 | Parsed | RegenerateHpOverTime | Boolean | ETC bit 10 -> passive HP regen |
| 2 | Parsed | VNumRequired | Int16 | VNum of the item required to tame/interact |
| 3 | NonParsed | unknown1 |  |  |
| 4 | Parsed | IsPercentileDmg | Boolean | Damage output is expressed as % of target max HP (boss mechanic) |
| 5 | Parsed | CanOnlyBeDmgedByJajamaruLastSkill | Boolean | Only damageable by Jajamaru's final skill (Namaju) |
| 6 | NonParsed | unknown2 |  | Always 0. |
| 7 | Parsed | VisibleOnMinimapAsGreenDot | Boolean | Renders as a green dot on the minimap |
| 8 | NonParsed | unknown3 |  |  |
| 9 | Parsed | IsValhallaPartner | Boolean | Marks Valhalla raid partner mobs |

## EXP

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | HeroXp | Int32 | Hero-level XP award, scaled down from XP |
| 2 | Parsed | Xp | Int32 | Base XP award |
| 3 | Parsed | JobXp | Int32 | Base job XP award |

## HP/MP

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | MaxHp | Int32 | Override HP plus level-based baseline |
| 3 | Parsed | MaxMp | Int32 | Override MP plus level-based baseline, branch on Race |

## ITEM

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 3 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 4 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 5 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 6 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 7 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 8 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 9 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 10 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 11 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 12 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 13 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 14 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 15 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 16 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 17 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 18 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 19 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 20 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 21 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 22 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 23 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 24 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 25 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 26 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 27 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 28 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 29 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 30 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 31 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 32 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 33 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 34 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 35 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 36 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 37 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 38 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 39 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 40 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 41 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 42 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 43 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 44 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 45 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 46 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 47 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 48 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 49 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 50 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 51 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 52 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 53 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 54 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 55 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 56 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 57 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 58 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 59 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 60 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 61 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |

## LEVEL

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | DamageMaximum | Int16 | Max attack damage |
| 2 | Parsed | DamageMinimum | Int16 | Min attack damage |
| 2 | Parsed | HeroXp | Int32 | Hero-level XP award, scaled down from XP |
| 2 | Parsed | JobXp | Int32 | Base job XP award |
| 2 | Parsed | Level | Byte | Parsed by Level(chunk) helper |
| 2 | Parsed | MaxHp | Int32 | Override HP plus level-based baseline |
| 2 | Parsed | MaxMp | Int32 | Override MP plus level-based baseline, branch on Race |
| 2 | Parsed | Xp | Int32 | Base XP award |

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
| 2 | Parsed | PetInfoVal1 | Int32 | Pet stat factor 1 (collectable: tries; teleporter: itemVnum; mate: HP/melee factor) |
| 2 | Parsed | VNumRequired | Int16 | VNum of the item required to tame/interact |
| 3 | Parsed | AmountRequired | Byte | Amount of the required item |
| 3 | Parsed | PetInfoVal2 | Int32 | Pet stat factor 2 (collectable: cooldown; teleporter: amount; mate: ranged/dodge factor) |
| 4 | Parsed | PetInfoVal3 | Int32 | Pet stat factor 3 (collectable: needItem flag; teleporter: removeItem; mate: magic/MP factor) |
| 5 | Parsed | PetInfoVal4 | Int32 | Pet stat factor 4 (collectable: dance duration; mate: level threshold) |

## PREATT

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | IsHostile | Boolean | Non-zero means the mob aggroes on sight |
| 3 | Parsed | GroupAttack | Byte | Group-aggro mode (1 = with allies, 5 = with group) |
| 4 | Parsed | NoticeRange | Byte | Aggro radius in cells |
| 5 | Parsed | Speed | Byte | Movement speed |
| 6 | Parsed | RespawnTime | Int32 | Respawn delay, deciseconds |

## RACE

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | MaxMp | Int32 | Override MP plus level-based baseline, branch on Race |
| 2 | Parsed | MonsterType | MonsterType | Categorisation (Mate/Normal/Trap/Unknown) |
| 2 | Parsed | NoAggresiveIcon | Boolean | Talkable / trap entities: suppress the aggressive icon |
| 2 | Parsed | Race | Byte | Race group (plant, animal, monster, dragon, …) |
| 3 | Parsed | MonsterType | MonsterType | Categorisation (Mate/Normal/Trap/Unknown) |
| 3 | Parsed | NoAggresiveIcon | Boolean | Talkable / trap entities: suppress the aggressive icon |
| 3 | Parsed | RaceType | Byte | Subtype within the race group |

## SETTING

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | IconId | Int16 | Target-info avatar index (NSip) |
| 3 | Parsed | SpawnMobOrColor | Int32 | When hostility>20000: spawn-mob vnum, else packed RGBA color |
| 4 | Parsed | AmountRequired | Byte | Amount of the required item |
| 4 | Parsed | VNumRequired | Int16 | VNum of the item required to tame/interact |
| 5 | Parsed | SpriteSize | SByte | Sprite scale bonus % (negative shrinks, positive grows) |
| 6 | Parsed | CellSize | Byte | Collision cell footprint (used for big-boss AOE math) |
| 7 | NonParsed | unknown0 |  | Always 0. |

## SKILL

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 3 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 4 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 5 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 6 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 7 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 8 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 9 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 10 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 11 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 12 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 13 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 14 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 15 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 16 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |

## VNUM

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | BCards | ICollection`1 | BCards from BASIC (10 groups of 5) and CARD (4 groups of 5) |
| 2 | Parsed | Drop | ICollection`1 | 20 group drops, vnum/chance/amount triples, merged with the per-monster drop DAO |
| 2 | Parsed | MonsterType | MonsterType | Categorisation (Mate/Normal/Trap/Unknown) |
| 2 | Parsed | NpcMonsterSkill | ICollection`1 | Up to 5 NpcMonsterSkill entries (vnum/chance/force triples) |
| 2 | Parsed | NpcMonsterVNum | Int16 | Monster vnum |

## WEAPON

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | Concentrate | Int16 | Hit rate |
| 2 | Parsed | DamageMaximum | Int16 | Max attack damage |
| 2 | Parsed | DamageMinimum | Int16 | Min attack damage |
| 3 | Parsed | Concentrate | Int16 | Hit rate |
| 3 | Parsed | CriticalChance | Byte | Critical hit chance |
| 3 | Parsed | CriticalRate | Int16 | Critical hit damage multiplier |
| 3 | Parsed | DamageMaximum | Int16 | Max attack damage |
| 3 | Parsed | DamageMinimum | Int16 | Min attack damage |
| 4 | Parsed | DamageMaximum | Int16 | Max attack damage |
| 4 | Parsed | DamageMinimum | Int16 | Min attack damage |
| 5 | Parsed | DamageMaximum | Int16 | Max attack damage |
| 6 | Parsed | Concentrate | Int16 | Hit rate |
| 7 | Parsed | CriticalChance | Byte | Critical hit chance |
| 8 | Parsed | CriticalRate | Int16 | Critical hit damage multiplier |

## WINFO

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | AttackUpgrade | Byte | Weapon upgrade level |
| 2 | Parsed | DefenceUpgrade | Byte | Armor upgrade level |
| 3 | NonParsed | unknown5 |  |  |
| 4 | Parsed | AttackUpgrade | Byte | Weapon upgrade level |

## ZSKILL

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | AttackClass | Byte | Attack class (melee/ranged/magic) |
| 3 | Parsed | BasicRange | Byte | Basic attack range in cells |
| 4 | Parsed | BasicHitChance | Byte | Basic-attack hit chance (each tick rolls hitChance*20%) |
| 5 | Parsed | BasicArea | Byte | Basic attack area-of-effect radius |
| 6 | Parsed | BasicCooldown | Int16 | Basic attack cooldown, deciseconds |
| 7 | Parsed | DashSpeed | Byte | Dash visual speed |
| 8 | NonParsed | unknown4 |  |  |

