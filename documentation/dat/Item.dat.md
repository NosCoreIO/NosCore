# Item.dat

```text
	FLAG	0	0	0	{IsSoldable}	{IsDroppable}	{IsTradable}	{IsMinilandActionable}	{IsWarehouse}	{Flag9}	{Flag1}	{Flag2}	{Flag3}	{Flag4}	{RequireBinding}	{IsColored}	{FemaleOnly}	{MaleOnly}	0	{Flag6}	0	{IsHeroic}	{Flag7}	{Flag8}	{RaidItem}	{UnknownLastBit}
	INDEX	0	0	{ItemSubType}
	NAME	{NameI18NKey}
	VNUM	{VNum}	{Price}
```

## FLAG

25 boolean bits. FLAG[25] identifies raid-inventory items (seals, boxes, chests, drops).

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Unknown |  |  |  |
| 3 | Unknown |  |  |  |
| 4 | Unknown |  |  |  |
| 5 | Parsed | IsSoldable | Boolean | Inverted: dat=0 -> soldable |
| 6 | Parsed | IsDroppable | Boolean | Inverted: dat=0 -> droppable |
| 7 | Parsed | IsTradable | Boolean | Inverted: dat=0 -> tradable |
| 8 | Parsed | IsMinilandActionable | Boolean | Miniland-actionable object |
| 9 | Parsed | IsWarehouse | Boolean | Can be stored in the account warehouse |
| 10 | Parsed | Flag9 | Boolean | FLAG bit 9 (appears on beads, medals, raid seals) |
| 11 | Parsed | Flag1 | Boolean | FLAG bit 10 (unknown) |
| 12 | Parsed | Flag2 | Boolean | FLAG bit 11 (set on mounts + raid items + partner equipment) |
| 13 | Parsed | Flag3 | Boolean | FLAG bit 12 (unknown) |
| 14 | Parsed | Flag4 | Boolean | FLAG bit 13 (unknown) |
| 15 | Parsed | RequireBinding | Boolean | Binds to character on use/equip |
| 16 | Parsed | IsColored | Boolean | Uses design slot for color |
| 17 | NonParsed | FemaleOnly |  | Set together with FLAG[18]=0 to mark a female-only item; rolled up into Sex. |
| 18 | NonParsed | MaleOnly |  | Set together with FLAG[17]=0 to mark a male-only item; rolled up into Sex. |
| 19 | Unknown |  |  |  |
| 20 | Parsed | Flag6 | Boolean | FLAG bit 19 (unknown) |
| 21 | Unknown |  |  |  |
| 22 | Parsed | IsHeroic | Boolean | Heroic item |
| 23 | Parsed | Flag7 | Boolean | FLAG bit 22 (unknown) |
| 24 | Parsed | Flag8 | Boolean | FLAG bit 23 (unknown) |
| 25 | NonParsed | RaidItem |  | Set on raid seals, raid boxes, chests and drops â€” routed to the Raid pocket. |
| 26 | NonParsed | UnknownLastBit |  |  |

## INDEX

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Unknown |  |  |  |
| 3 | Unknown |  |  |  |
| 4 | Parsed | ItemSubType | Byte | Subtype within item category |

## NAME

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | NameI18NKey | String | Localization key (zts##e) |

## VNUM

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | VNum | Int16 | Item vnum |
| 3 | Parsed | Price | Int64 | NPC shop sell price |

## Computed / multi-section fields

| DTO property | Type | Source | Description |
|---|---|---|---|
| BasicUpgrade | Byte | DATA[varies] | Base upgrade level |
| BCards | ICollection`1 | BUFF[0..24] | Up to 5 BCard groups (5 fields each) |
| Class | Byte | TYPE[1] (fairy override: 15) | Class restriction bitmask |
| CloseDefence | Int16 | DATA[varies] | Melee defence |
| CriticalLuckRate | Byte | DATA[varies] | Critical luck multiplier |
| CriticalRate | Int16 | DATA[varies] | Critical hit chance |
| DamageMaximum | Int16 | DATA[varies] | Max weapon damage |
| DamageMinimum | Int16 | DATA[varies] | Min weapon damage |
| DarkResistance | Int16 | DATA[11] or DATA[18] | Dark resistance; SP uses DATA[18] |
| DefenceDodge | Int16 | DATA[varies] | Dodge chance |
| DistanceDefence | Int16 | DATA[varies] | Ranged defence |
| DistanceDefenceDodge | Int16 | DATA[varies] | Ranged dodge chance |
| Effect | ItemEffectType | DATA[varies] per ItemType | ItemEffectType enum |
| EffectValue | Int32 | DATA[varies] per ItemType | Effect numeric argument |
| Element | ElementType | DATA[varies] | Primary elemental alignment |
| ElementRate | Int16 | DATA[varies] | Element rate % |
| EquipmentSlot | EquipmentType | INDEX[3] | Equipment slot |
| FireResistance | Int16 | DATA[7] or DATA[15] | Fire resistance; SP uses DATA[15], others DATA[7] |
| Height | Byte | DATA[4] (miniland only) | Miniland object footprint height |
| HitRate | Int16 | DATA[varies] | Hit rate |
| Hp | Int16 | DATA[varies] | Hp bonus on equip |
| ItemType | ItemType | INDEX[0,1] | ItemType composite enum |
| ItemValidTime | Int64 | DATA[varies] | Expiration duration in seconds |
| LevelJobMinimum | Byte | DATA[varies] | Minimum job level to use/equip |
| LevelMinimum | Byte | DATA[varies] | Minimum level to equip/use, per-ItemType |
| LightResistance | Int16 | DATA[9] or DATA[17] | Light resistance; SP uses DATA[17] |
| MagicDefence | Int16 | DATA[varies] | Magic defence |
| MaxCellon | Byte | DATA[varies] | Max cellons slotted |
| MaxCellonLvl | Byte | DATA[varies] | Max cellon level |
| MaximumAmmo | Byte | DATA[varies] | Max ammo |
| MinilandObjectPoint | Int32 | DATA[2] (miniland only) | Miniland point value |
| Morph | Int16 | INDEX[3] + INDEX[5] | Morph id (partner skin) or effect value |
| Mp | Int16 | DATA[varies] | Mp bonus on equip |
| ReputationMinimum | Byte | DATA[varies] | Minimum reputation tier |
| ReputPrice | Int64 | FLAG[20] + VNUM[1] | Reputation price when FLAG[20] is set |
| Sex | Byte | FLAG[16] + FLAG[17] | Gender restriction: 1=female, 2=male, 0=any |
| Speed | Byte | DATA[varies] | Movement speed bonus / vehicle speed |
| SpType | Byte | DATA[varies] (SP only) | SP transformation type |
| Type | NoscorePocketType | FLAG[25] + DATA[0] + INDEX[0,1] | Pocket type: FLAG[25]==1 -> Raid, DATA[0]==1000 && INDEX[1]==4 -> Mount, else legacy INDEX[0] map |
| WaitDelay | Int16 | DATA[varies] | Cooldown / use delay |
| WaterResistance | Int16 | DATA[8] or DATA[16] | Water resistance; SP uses DATA[16] |
| Width | Byte | DATA[3] (miniland only) | Miniland object footprint width |

