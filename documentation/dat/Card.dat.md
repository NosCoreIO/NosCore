# Card.dat

```text
	EFFECT	{EffectId}
	GROUP	0	{Level}
	LAST	{TimeoutBuff}	{TimeoutBuffChance}
	NAME	{NameI18NKey}
	STYLE	0	{BuffType}
	TIME	{Duration}	{Delay}
	VNUM	{CardId}
```

## EFFECT

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | EffectId | Int32 | Visual effect id |

## GROUP

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Unknown |  |  |  |
| 3 | Parsed | Level | Byte | Card level tier |

## LAST

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | TimeoutBuff | Int16 | Follow-up buff id when card expires |
| 3 | Parsed | TimeoutBuffChance | Byte | % chance the follow-up buff fires |

## NAME

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | NameI18NKey | String | Localization key (zts##e) |

## STYLE

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Unknown |  |  |  |
| 3 | Parsed | BuffType | CardType | Buff type from STYLE column 3 |

## TIME

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | Duration | Int32 | Duration in deciseconds |
| 3 | Parsed | Delay | Int32 | Activation delay |

## VNUM

| Column | Status | Name | Type | Description |
|---:|---|---|---|---|
| 2 | Parsed | CardId | Int16 | Card vnum |

## Computed / multi-section fields

| DTO property | Type | Source | Description |
|---|---|---|---|
| BCards | ICollection`1 | 1ST + 2ST (5 groups of 6) | Up to 5 BCards, first 3 from 1ST then 2 from 2ST |

