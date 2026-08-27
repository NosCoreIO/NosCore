# BCard.dat

Every effect `BCard.dat` declares, and the `BCardEffect` member that names it.
`Declared` is `no` for members no client file on record declares.
Regenerate with `NOSCORE_DAT_FOLDER=<folder> dotnet test --filter BCardVocabularyTests.Regenerate`.

| Type | Subtype | Declared | Effect | Client text |
|---:|---:|---|---|---|
| 1 | 11 | yes | SpecialAttackNoAttack | No attack possible |
| 1 | 12 | yes | SpecialAttackNoAttackNegated | No attack possible |
| 1 | 21 | yes | SpecialAttackMeleeDisabled | Melee attacks are not allowed. |
| 1 | 22 | yes | SpecialAttackMeleeDisabledNegated | Melee attacks are not allowed. |
| 1 | 31 | yes | SpecialAttackRangedDisabled | Ranged attacks are not allowed. |
| 1 | 32 | yes | SpecialAttackRangedDisabledNegated | Ranged attacks are not allowed. |
| 1 | 41 | yes | SpecialAttackMagicDisabled | Magic attacks are not allowed. |
| 1 | 42 | yes | SpecialAttackMagicDisabledNegated | Magic attacks are not allowed. |
| 1 | 51 | yes | SpecialAttackFailIfMiss | The attack fails if you miss the target. (Excluding magic attacks.) |
| 1 | 52 | yes | SpecialAttackFailIfMissNegated | The attack fails if you miss the target. (Excluding magic attacks.) |
| 2 | 11 | yes | SpecialDefenceAllDefenceNullified | All defence is nullified. |
| 2 | 12 | yes | SpecialDefenceAllDefenceNullifiedNegated | All defence is nullified. |
| 2 | 21 | yes | SpecialDefenceMeleeDefenceNullified | Melee defence is nullified. |
| 2 | 22 | yes | SpecialDefenceMeleeDefenceNullifiedNegated | Melee defence is nullified. |
| 2 | 31 | yes | SpecialDefenceRangedDefenceNullified | Ranged defence is nullified. |
| 2 | 32 | yes | SpecialDefenceRangedDefenceNullifiedNegated | Ranged defence is nullified. |
| 2 | 41 | yes | SpecialDefenceMagicDefenceNullified | Magic defence is nullified. |
| 2 | 42 | yes | SpecialDefenceMagicDefenceNullifiedNegated | Magic defence is nullified. |
| 2 | 51 | yes | SpecialDefenceNoDefence | No defence possible |
| 2 | 52 | yes | SpecialDefenceNoDefenceNegated | No defence possible |
| 3 | 11 | yes | AttackPowerAllAttacksIncreased | All attacks are increased by %s. |
| 3 | 12 | yes | AttackPowerAllAttacksDecreased | All attacks are decreased by %s. |
| 3 | 21 | yes | AttackPowerMeleeAttacksIncreased | Melee attack is increased by %s. |
| 3 | 22 | yes | AttackPowerMeleeAttacksDecreased | Melee attack is reduced by %s. |
| 3 | 31 | yes | AttackPowerRangedAttacksIncreased | Ranged attack is increased by %s. |
| 3 | 32 | yes | AttackPowerRangedAttacksDecreased | Ranged attack is reduced by %s. |
| 3 | 41 | yes | AttackPowerMagicalAttacksIncreased | Magic attack power is increased by %s. |
| 3 | 42 | yes | AttackPowerMagicalAttacksDecreased | Magic attack power is decreased by %s. |
| 3 | 51 | yes | AttackPowerAttackLevelIncreased | Attack level is increased by %s. |
| 3 | 52 | yes | AttackPowerAttackLevelDecreased | Attack level is decreased by %s. |
| 4 | 11 | yes | TargetAllHitRateIncreased | Hit rate of all attacks is increased by %s. |
| 4 | 12 | yes | TargetAllHitRateDecreased | Hit rate of all attacks is decreased by %s. |
| 4 | 21 | yes | TargetMeleeHitRateIncreased | Hit rate of melee attacks is increased by %s. |
| 4 | 22 | yes | TargetMeleeHitRateDecreased | Hit rate of melee attacks is decreased by %s. |
| 4 | 31 | yes | TargetRangedHitRateIncreased | Hit rate of ranged attacks is increased by %s. |
| 4 | 32 | yes | TargetRangedHitRateDecreased | Hit rate of ranged attacks is decreased by %s. |
| 4 | 41 | yes | TargetMagicalConcentrationIncreased | Concentration is increased by %s during the magic attack. |
| 4 | 42 | yes | TargetMagicalConcentrationDecreased | Concentration is decreased by %s during the magic attack. |
| 4 | 51 | yes |  | Damage to monsters is increased by %s%%. |
| 5 | 11 | yes | CriticalInflictingIncreased | Chance of inflicting critical hits is increased by %s%%. |
| 5 | 12 | yes | CriticalInflictingReduced | Probability of causing a critical hit is reduced by %s%%. |
| 5 | 21 | yes | CriticalDamageIncreased | Increases damage from critical hits by %s%%. |
| 5 | 22 | yes | CriticalDamageIncreasedInflictingReduced | When inflicting critical hits, damage is reduced by %s%%. |
| 5 | 31 | yes | CriticalDamageIncreasingPropability | Increases damage with a probability of %s%% by %s%%. |
| 5 | 32 | yes | CriticalDamageReducingPropability | On attack there is a %s%% chance of damage being reduced by %s%%. |
| 5 | 41 | yes | CriticalReceivingIncreased | The chance of receiving a critical hit is increased by %s%%. |
| 5 | 42 | yes | CriticalReceivingDecreased | Probability to receive critical hits is decreased by %s%%. |
| 5 | 51 | yes | CriticalDamageFromCriticalIncreased | Damage taken from critical hits is increased by %s%%. |
| 5 | 52 | yes | CriticalDamageFromCriticalDecreased | Damage from critical hits is reduced by %s%%. |
| 6 | 11 | yes | SpecialCriticalAlwaysInflict | Always inflicts critical hits. |
| 6 | 12 | yes | SpecialCriticalAlwaysInflictNegated | Always inflicts critical hits. |
| 6 | 21 | yes | SpecialCriticalNeverInflict | Never inflicts critical hits. |
| 6 | 22 | yes | SpecialCriticalNeverInflictNegated | Never inflicts critical hits. |
| 6 | 31 | yes | SpecialCriticalAlwaysReceives | Always receives critical hits. |
| 6 | 32 | yes | SpecialCriticalAlwaysReceivesNegated | Always receives critical hits. |
| 6 | 41 | yes | SpecialCriticalNeverReceives | Never receives critical hits. |
| 6 | 42 | yes | SpecialCriticalNeverReceivesNegated | Never receives critical hits. |
| 6 | 51 | yes | SpecialCriticalInflictingChancePercent | Inflicts critical hits with a chance of %s%%. |
| 6 | 52 | yes | SpecialCriticalReceivingChancePercent | Receives a critical hit with the chance of %s%%. |
| 7 | 11 | yes | ElementFireIncreased | Fire element is increased by %s. |
| 7 | 12 | yes | ElementFireDecreased | Fire element is decreased by %s. |
| 7 | 21 | yes | ElementWaterIncreased | Water element is increased by %s. |
| 7 | 22 | yes | ElementWaterDecreased | Water element is decreased by %s. |
| 7 | 31 | yes | ElementLightIncreased | Light element is increased by %s. |
| 7 | 32 | yes | ElementLightDecreased | Light element is decreased by %s. |
| 7 | 41 | yes | ElementDarkIncreased | Shadow element is increased by %s. |
| 7 | 42 | yes | ElementDarkDecreased | Shadow element is decreased by %s. |
| 7 | 51 | yes | ElementAllIncreased | All element energies are increased by %s. |
| 7 | 52 | yes | ElementAllDecreased | All element energies are reduced by %s. |
| 8 | 11 | yes | IncreaseDamageIncreasingPropability | Provides a %s%% chance to increase attack power by %s%%. |
| 8 | 12 | yes | IncreaseDamageDecreasingPropability | Provides a %s%% chance to reduce damage by %s%%. |
| 8 | 21 | yes | IncreaseDamageFireIncreased | Fire element is increased by %s. |
| 8 | 22 | yes | IncreaseDamageFireDecreased | Fire element is decreased by %s. |
| 8 | 31 | yes | IncreaseDamageWaterIncreased | Water element is increased by %s. |
| 8 | 32 | yes | IncreaseDamageWaterDecreased | Water element is decreased by %s. |
| 8 | 41 | yes | IncreaseDamageLightIncreased | Light element is increased by %s. |
| 8 | 42 | yes | IncreaseDamageLightDecreased | Light element is decreased by %s. |
| 8 | 51 | yes | IncreaseDamageDarkIncreased | Shadow element is increased by %s. |
| 8 | 52 | yes | IncreaseDamageDarkDecreased | Shadow element is decreased by %s. |
| 9 | 11 | yes | DefenceAllIncreased | All defence powers are increased by %s. |
| 9 | 12 | yes | DefenceAllDecreased | All defence powers are decreased by %s. |
| 9 | 21 | yes | DefenceMeleeIncreased | Melee defence is increased by %s. |
| 9 | 22 | yes | DefenceMeleeDecreased | Melee defence is decreased by %s. |
| 9 | 31 | yes | DefenceRangedIncreased | Ranged defence is increased by %s. |
| 9 | 32 | yes | DefenceRangedDecreased | Ranged defence is decreased by %s. |
| 9 | 41 | yes | DefenceMagicalIncreased | Magic defence is increased by %s. |
| 9 | 42 | yes | DefenceMagicalDecreased | Magic defence is reduced by %s. |
| 9 | 51 | yes | DefenceDefenceLevelIncreased | Defence level is increased by %s. |
| 9 | 52 | yes | DefenceDefenceLevelDecreased | Defence level is decreased by %s. |
| 10 | 11 | yes | DodgeAndDefencePercentDodgeIncreased | Dodge is increased by %s. |
| 10 | 12 | yes | DodgeAndDefencePercentDodgeDecreased | Dodge is decreased by %s. |
| 10 | 21 | yes | DodgeAndDefencePercentDodgingMeleeIncreased | Dodging of melee attacks is increased by %s. |
| 10 | 22 | yes | DodgeAndDefencePercentDodgingMeleeDecreased | Dodging of melee attacks is decreased by %s. |
| 10 | 31 | yes | DodgeAndDefencePercentDodgingRangedIncreased | Dodging of ranged attacks is increased by %s. |
| 10 | 32 | yes | DodgeAndDefencePercentDodgingRangedDecreased | Dodging of ranged attacks is decreased by %s. |
| 10 | 41 | yes | DodgeAndDefencePercentDefenceIncreased | Defence is increased by %s%%. |
| 10 | 42 | yes | DodgeAndDefencePercentDefenceReduced | Defence power is reduced by %s%%. |
| 10 | 51 | yes |  | Increases damage to all monsters in Act 8 (excluding raids) and the Act 8 Land of Life by %s%%. |
| 10 | 52 | yes |  | Reduces damage from all monsters in Act 8 (excluding raids) and the Act 8 Land of Life by %s%%. |
| 11 | 11 | yes | BlockChanceAllIncreased | There is a %s%% chance that damage taken from all attacks is increased by %s%%. |
| 11 | 12 | yes | BlockChanceAllDecreased | There is a %s%% chance that damage from all attacks is reduced by %s%%. |
| 11 | 21 | yes | BlockChanceMeleeIncreased | There is a %s%% chance that damage taken from melee attacks is increased by %s%%. |
| 11 | 22 | yes | BlockChanceMeleeDecreased | There is a %s%% chance that damage from melee attacks is reduced by %s%%. |
| 11 | 31 | yes | BlockChanceRangedIncreased | There is a %s%% chance that damage taken from ranged attacks is increased by %s%%. |
| 11 | 32 | yes | BlockChanceRangedDecreased | There is a %s%% chance that damage from ranged attacks is reduced by %s%%. |
| 11 | 41 | yes | BlockChanceMagicalIncreased | There is a %s%% chance that damage taken from magic attacks is increased by %s%%. |
| 11 | 42 | yes | BlockChanceMagicalDecreased | There is a %s%% chance that damage from magic attacks is reduced by %s%%. |
| 11 | 51 | yes | BlockCriticalDamageReducedPerHit | Final damage from incoming critical hits is reduced by %s%% per critical hit (max. %s hits). |
| 11 | 52 | yes | BlockCriticalDamageReducedPerHitNegated | Final damage from incoming critical hits is reduced by %s%% per critical hit (max. %s hits). |
| 12 | 11 | yes | AbsorptionAllAttackIncreased | All attacks below level (+%s) cause %s%% more damage. |
| 12 | 12 | yes | AbsorptionAllAttackDecreased | All attacks below level (+%s) cause %s%% less damage. |
| 12 | 21 | yes | AbsorptionMeleeAttackIncreased | Melee attacks below level (+%s) cause %s%% more damage. |
| 12 | 22 | yes | AbsorptionMeleeAttacklDecreased | Melee attacks below level (+%s) cause %s%% less damage. |
| 12 | 31 | yes | AbsorptionRangedAttackIncreased | Ranged attacks below level (+%s) cause %s%% more damage. |
| 12 | 32 | yes | AbsorptionRangedAttackDecreased | Ranged attacks below level (+%s) cause %s%% less damage. |
| 12 | 41 | yes | AbsorptionMagicalAttackIncreased | Magic attacks below level (+%s) cause %s%% more damage. |
| 12 | 42 | yes | AbsorptionMagicalAttacksDecreased | Magic attacks below level (+%s) cause %s%% less damage. |
| 12 | 51 | yes |  | Absorb %s%% of your max HP as damage. |
| 12 | 52 | yes |  | Absorb %s%% of your max HP as damage. |
| 13 | 11 | yes | ElementResistanceAllIncreased | All elemental resistance is increased by %s. |
| 13 | 12 | yes | ElementResistanceAllDecreased | All elemental resistances are decreased by %s. |
| 13 | 21 | yes | ElementResistanceFireIncreased | Fire resistance is increased by %s. |
| 13 | 22 | yes | ElementResistanceFireDecreased | Fire resistance is decreased by %s. |
| 13 | 31 | yes | ElementResistanceWaterIncreased | Water resistance is increased by %s. |
| 13 | 32 | yes | ElementResistanceWaterDecreased | Water resistance is decreased by %s. |
| 13 | 41 | yes | ElementResistanceLightIncreased | Light resistance is increased by %s. |
| 13 | 42 | yes | ElementResistanceLightDecreased | Light resistance is decreased by %s. |
| 13 | 51 | yes | ElementResistanceDarkIncreased | Shadow resistance is increased by %s. |
| 13 | 52 | yes | ElementResistanceDarkDecreased | Shadow resistance is decreased by %s. |
| 14 | 11 | yes | EnemyElementResistanceAllIncreased | Increases the enemy's elemental resistances by %s. |
| 14 | 12 | yes | EnemyElementResistanceAllDecreased | Reduces the enemy's elemental resistances by %s. |
| 14 | 21 | yes | EnemyElementResistanceFireIncreased | Increases the enemy's fire resistance by %s. |
| 14 | 22 | yes | EnemyElementResistanceFireDecreased | Reduces the enemy's fire resistance by %s. |
| 14 | 31 | yes | EnemyElementResistanceWaterIncreased | Increases the enemy's water resistance by %s. |
| 14 | 32 | yes | EnemyElementResistanceWaterDecreased | Reduces the enemy's water resistance by %s. |
| 14 | 41 | yes | EnemyElementResistanceLightIncreased | Increases the enemy's light resistance by %s. |
| 14 | 42 | yes | EnemyElementResistanceLightDecreased | Reduces the enemy's light resistance by %s. |
| 14 | 51 | yes | EnemyElementResistanceDarkIncreased | Increases the enemy's shadow resistance by %s. |
| 14 | 52 | yes | EnemyElementResistanceDarkDecreased | Reduces the enemy's shadow resistance by %s. |
| 15 | 11 | yes | DamageDamageIncreased | Damage taken from all attacks is increased by %s%%. |
| 15 | 12 | yes | DamageDamageDecreased | Damage taken is reduced by %s%%. |
| 15 | 21 | yes | DamageMeleeIncreased | Damage taken from melee attacks is increased by %s%%. |
| 15 | 22 | yes | DamageMeleeDecreased | Melee damage is decreased by %s%%. |
| 15 | 31 | yes | DamageRangedIncreased | Damage taken from ranged attacks is increased by %s%%. |
| 15 | 32 | yes | DamageRangedDecreased | Ranged damage is decreased by %s%%. |
| 15 | 41 | yes | DamageMagicalIncreased | Damage taken from magic attacks is increased by %s%%. |
| 15 | 42 | yes | DamageMagicalDecreased | Magic damage is decreased by %s%%. |
| 15 | 51 | yes |  | Increases HP by %s%% of max. HP for every attack received (max. %s times). |
| 15 | 52 | yes |  | Increases MP by %s%% of max. MP for every attack received (max. %s times). |
| 16 | 11 | yes | GuarantedDodgeRangedAttackAttackHitChance | There is a %s%% chance that every attack hits. |
| 16 | 12 | yes | GuarantedDodgeRangedAttackAttackHitChanceNegated | There is a %s%% chance that every attack hits. |
| 16 | 21 | yes | GuarantedDodgeRangedAttackAlwaysDodgePropability | Always dodge the target with a probability of %s%%. |
| 16 | 22 | yes | GuarantedDodgeRangedAttackAlwaysDodgePropabilityNegated | Always dodge the target with a probability of %s%%. |
| 16 | 31 | yes | GuarantedDodgeRangedAttackNoPenalty | No penalty for ranged attacks at close range. |
| 16 | 32 | yes | GuarantedDodgeRangedAttackNoPenaltyNegated | No penalty for ranged attacks at close range. |
| 16 | 41 | yes | GuarantedDodgeRangedAttackDistanceDamageIncreasing | Ranged attack power increases with the distance to the enemy. |
| 16 | 42 | yes | GuarantedDodgeRangedAttackDistanceDamageIncreasingNegated | Ranged attack power increases with the distance to the enemy. |
| 16 | 51 | yes | GuarantedDodgeRangedAttackFairyDamagePerDebuff | The damage from the equipped fairy is increased by %s per debuff. |
| 17 | 11 | yes | MoraleMoraleIncreased | Morale stat is increased by %s. |
| 17 | 12 | yes | MoraleMoraleDecreased | Morale stat is decreased by %s. |
| 17 | 21 | yes | MoraleMoraleDoubled | Morale stat is doubled. |
| 17 | 22 | yes | MoraleMoraleHalved | Morale stat is halved. |
| 17 | 31 | yes | MoraleLockMorale | No impact on morale stat. |
| 17 | 32 | yes | MoraleLockMoraleNegated | No impact on morale stat. |
| 17 | 41 | yes | MoraleSkillCooldownIncreased | Skill cooldown is increased by %s%%. |
| 17 | 42 | yes | MoraleSkillCooldownDecreased | Reduces skill cooldowns by %s%% (max. 80%%). |
| 17 | 51 | yes | MoraleIgnoreEnemyMorale | Ignores the enemy's morale when attacking. |
| 17 | 52 | yes | MoraleIgnoreEnemyMoraleNegated | Ignores the enemy's morale when attacking. |
| 18 | 11 | yes | CastingEffectDurationIncreased | Effect duration is increased by %s%%. |
| 18 | 12 | yes | CastingEffectDurationDecreased | Effect duration is decreased by %s%%. |
| 18 | 21 | yes | CastingManaForSkillsIncreased | Mana for using skills is increased by %s%%. (Includes magic.) |
| 18 | 22 | yes | CastingManaForSkillsDecreased | Mana for using skills is decreased by %s%%. (Includes magic.) |
| 18 | 31 | yes | CastingAttackSpeedIncreased | Attack speed is increased by %s%%. |
| 18 | 32 | yes | CastingAttackSpeedDecreased | Attack speed is decreased by %s%%. |
| 18 | 41 | yes | CastingCastingSkillFailed | Casting skill failed (including magic) |
| 18 | 42 | yes | CastingCastingSkillFailedNegated | Casting skill failed (including magic) |
| 18 | 51 | yes | CastingInterruptCasting | Interrupt casting (including magic) |
| 18 | 52 | yes | CastingInterruptCastingNegated | Interrupt casting (including magic) |
| 19 | 11 | yes | MoveMovementImpossible | Movement impossible |
| 19 | 12 | yes | MoveMovementImpossibleNegated | Movement impossible |
| 19 | 21 | yes | MoveMoveSpeedIncreased | Movement speed is increased by %s%%. |
| 19 | 22 | yes | MoveMoveSpeedDecreased | Movement speed is decreased by %s%%. |
| 19 | 31 | yes | MoveSpeedWhileHiddenIncreased | Your movement speed is increased by %s while you are hidden. |
| 19 | 32 | yes | MoveSpeedWhileHiddenDecreased | Your movement speed is decreased by %s while you are hidden. |
| 19 | 41 | yes | MoveMovementSpeedIncreased | Movement speed is increased by %s. |
| 19 | 42 | yes | MoveMovementSpeedDecreased | Movement speed is decreased by %s. |
| 19 | 51 | yes | MoveTempMaximized | Your speed is temporarily maximised. |
| 19 | 52 | yes | MoveTempMaximizedNegated | Your speed is temporarily maximised. |
| 20 | 11 | yes | ReflectionHpIncreased | HP is increased by %s%% of damage given. |
| 20 | 12 | yes | ReflectionHpDecreased | HP is decreased by %s%% of the damage dealt (up to 50%% of the max. HP of the player with the buff). |
| 20 | 21 | yes | ReflectionMpIncreased | MP is increased by %s%% of damage given. |
| 20 | 22 | yes | ReflectionMpDecreased | MP is decreased by %s%% of damage given. |
| 20 | 31 | yes | ReflectionEnemyHpIncreased | Enemy's HP is increased by %s%% of the damage I have received. |
| 20 | 32 | yes | ReflectionEnemyHpDecreased | Enemy's HP is decreased by %s%% of the damage you take (up to 50%% of the max. HP of the player with the buff). |
| 20 | 41 | yes | ReflectionEnemyMpIncreased | Enemy's MP is increased by %s%% of the damage I have received. |
| 20 | 42 | yes | ReflectionEnemyMpDecreased | Enemy's MP is decreased by %s%% of the damage I have received. |
| 20 | 51 | yes |  | Do not use this part. |
| 20 | 52 | yes |  | There is a %s%% chance that %s%% of the remaining MP is lost. |
| 21 | 11 | yes | DrainAndStealReceiveHpFromMp | Receive %s HP for using %s MP. |
| 21 | 12 | yes | DrainAndStealReceiveHpFromMpNegated | Receive %s HP for using %s MP. |
| 21 | 21 | yes | DrainAndStealReceiveMpFromHp | Receive %s MP for using %s HP. |
| 21 | 22 | yes | DrainAndStealReceiveMpFromHpNegated | Receive %s MP for using %s HP. |
| 21 | 31 | yes | DrainAndStealGiveEnemyHp | There's a %s%% chance of giving your enemy %s HP. |
| 21 | 32 | yes | DrainAndStealLeechEnemyHp | There's a %s%% chance of leeching %s HP from the enemy. |
| 21 | 41 | yes | DrainAndStealGiveEnemyMp | There's a %s%% chance of giving your enemy %s MP. |
| 21 | 42 | yes | DrainAndStealLeechEnemyMp | There's a %s%% chance of leeching %s MP from your enemy. |
| 21 | 51 | yes | DrainAndStealConvertEnemyMptoHp | Converts %s of enemy's MP into HP. |
| 21 | 52 | yes | DrainAndStealConvertEnemyHptoMp | Converts %s HP from enemy into your MP. |
| 22 | 11 | yes | HealingBurningAndCastingRestoreHp | Restores %s HP. |
| 22 | 12 | yes | HealingBurningAndCastingDecreaseHp | HP is reduced by %s. |
| 22 | 21 | yes | HealingBurningAndCastingRestoreMp | Restores %s MP. |
| 22 | 22 | yes | HealingBurningAndCastingDecreaseMp | MP is reduced by %s. |
| 22 | 31 | yes | HealingBurningAndCastingRestoreHpWhenCasting | %s HP restored while casting. |
| 22 | 32 | yes | HealingBurningAndCastingDecreaseHpWhenCasting | %s HP lost while casting. |
| 22 | 41 | yes | HealingBurningAndCastingRestoreHpWhenCastingInterrupted | %s HP is restored when spell casting is interrupted. |
| 22 | 42 | yes | HealingBurningAndCastingDecreaseHpWhenCastingInterrupted | %s HP is lost when spell casting is interrupted. |
| 22 | 51 | yes | HealingBurningAndCastingHpIncreasedByConsumingMp | %s%% HP is recovered by consuming MP. |
| 22 | 52 | yes | HealingBurningAndCastingHpDecreasedByConsumingMp | Reduces HP by %s%% of MP consumed. |
| 23 | 11 | yes | HpmpRestoreDecreasedHp | Restores %s%% of HP lost. |
| 23 | 12 | yes | HpmpDecreaseRemainingHp | Reduces remaining HP by %s%%. |
| 23 | 21 | yes | HpmpRestoreDecreasedMp | Restores %s%% of MP lost. |
| 23 | 22 | yes | HpmpDecreaseRemainingMp | Reduces remaining MP by %s%%. |
| 23 | 31 | yes | HpmpHpRestored | %s HP is restored. |
| 23 | 32 | yes | HpmpHpReduced | HP is reduced by %s. |
| 23 | 41 | yes | HpmpMpRestored | %s MP is restored. |
| 23 | 42 | yes | HpmpMpReduced | MP is reduced by %s. |
| 23 | 51 | yes | HpmpReceiveAdditionalHp | You receive an additional %s HP. (Cannot exceed %s%% of your maximum HP.) |
| 23 | 52 | yes | HpmpReceiveAdditionalMp | You receive an additional %s MP. (Cannot exceed %s%% of your maximum MP.) |
| 24 | 11 | yes | SpecialisationBuffResistanceIncreaseDamageAgainst | Increases damage against %s by %s. |
| 24 | 12 | yes | SpecialisationBuffResistanceReduceDamageAgainst | If %s attacks a player, damage is reduced by %s. |
| 24 | 21 | yes | SpecialisationBuffResistanceIncreaseCriticalAgainst | Critical hit chance against %s is increased by %s. |
| 24 | 22 | yes | SpecialisationBuffResistanceReduceCriticalAgainst | If %s attacks a player, the critical hit chance is reduced by %s. |
| 24 | 31 | yes | SpecialisationBuffResistanceResistanceToEffect | %s%% resistance to the effect: %s and lower. |
| 24 | 32 | yes | SpecialisationBuffResistanceResistanceToEffectNegated | %s%% resistance to the effect: %s and lower. |
| 24 | 41 | yes | SpecialisationBuffResistanceIncreaseDamageInPvp | Increases damage in PvP by %s. |
| 24 | 42 | yes | SpecialisationBuffResistanceDecreaseDamageInPvp | Reduces damage in PvP by %s. |
| 24 | 51 | yes | SpecialisationBuffResistanceRemoveBuffBelowLevel | There is a %s%% chance that buffs below Lv. %s will be removed. |
| 24 | 52 | yes | SpecialisationBuffResistanceRemoveBadEffects | There is a %s%% probability to remove bad effects of level %s or lower. |
| 25 | 11 | yes | BuffChanceCausing | Has a %s%% probability of causing [%s]. |
| 25 | 12 | yes | BuffChanceRemoving | There is a %s%% chance that %s will be removed. |
| 25 | 21 | yes | BuffPreventingBadEffect | High probability of preventing a bad effect. |
| 25 | 22 | yes | BuffPreventingBadEffectNegated | High probability of preventing a bad effect. |
| 25 | 31 | yes | BuffNearbyObjectsAboveLevel | Nearby objects above level %s are given %s. |
| 25 | 32 | yes | BuffNearbyObjectsBelowLevel | Nearby objects below level %s are given %s. |
| 25 | 41 | yes | BuffEffectResistance | Generates resistance to a certain effect. |
| 25 | 42 | yes | BuffEffectResistanceNegated | Generates resistance to a certain effect. |
| 25 | 51 | yes | BuffCancelGroupOfEffects | Cancel effects for a certain group. |
| 25 | 52 | yes | BuffCounteractPoison | Counteracts effects of the poison for a certain target or group. |
| 26 | 11 | yes | SummonsSummonUponDeath | Summons %s of %s monsters upon death. |
| 26 | 12 | yes | SummonsSummonUponDeathChance | Upon death there is a %s%% chance of summoning %s. |
| 26 | 21 | yes | SummonsSummons | Summons %s x %s. |
| 26 | 22 | yes | SummonsSummonningChance | There's a %s%% chance %s will be summoned. |
| 26 | 31 | yes | SummonsSummonTrainingDummy | Summons %s of %s monsters. (Training dummy) |
| 26 | 32 | yes | SummonsSummonTrainingDummyChance | With a probability of %s%%, %s will be summoned. (Training dummy) |
| 26 | 41 | yes | SummonsSummonTimedMonsters | Summons %s of %s monsters. (Disappear after a set amount of time) |
| 26 | 42 | yes | SummonsSummonTimedMonstersChance | With a probability of %s%%, %s will be summoned. (Disappears after a set amount of time) |
| 26 | 51 | yes | SummonsSummonGhostMp | Summons a ghost with %s MP from %s monsters. |
| 26 | 52 | yes | SummonsSummonGhostMpChance | With a probability of %s%%, summons a ghost with %s MP. |
| 27 | 11 | yes | SpecialEffectsDecreaseKillerHp | Decreases HP of enemy that killed you by %s%%. |
| 27 | 12 | yes | SpecialEffectsIncreaseKillerHp | Increases HP of enemy that killed you by %s%%. |
| 27 | 21 | yes | SpecialEffectsToPrefferedAttack | Changes to preferred attack with a probability of %s%%. |
| 27 | 22 | yes | SpecialEffectsToNonPrefferedAttack | Changes to non-preferred attack with a probability of %s%%. |
| 27 | 31 | yes | SpecialEffectsGibberish | gibberish is spoken. |
| 27 | 32 | yes | SpecialEffectsGibberishNegated | gibberish is spoken. |
| 27 | 41 | yes | SpecialEffectsAbleToFightPvp | Able to fight against people in PvP mode. |
| 27 | 42 | yes | SpecialEffectsAbleToFightPvpNegated | Able to fight against people in PvP mode. |
| 27 | 51 | yes | SpecialEffectsShadowAppears | A shadowy figure appears. |
| 27 | 52 | yes | SpecialEffectsShadowAppearsNegated | A shadowy figure appears. |
| 28 | 11 | yes | CaptureCaptureAnimal | Capture animals that are of a lower level than you to turn them into your pets. |
| 28 | 12 | yes | CaptureCaptureAnimalNegated | Capture animals that are of a lower level than you to turn them into your pets. |
| 28 | 21 | yes |  | Can hunt sheep. |
| 28 | 22 | yes |  | Can hunt sheep. |
| 28 | 31 | yes |  | Throw the acorn as hard as you can. |
| 28 | 32 | yes |  | Throw the acorn as hard as you can. |
| 28 | 41 | yes |  | The pet gains %s%% more training experience. |
| 28 | 42 | yes |  | The pet gains %s%% more training experience. |
| 28 | 51 | yes |  | There's a %s%% chance to get an additional essence when extracting the essence from %s-star pets. |
| 28 | 52 | yes |  | There's a %s%% chance to get an additional essence when extracting the essence from %s-star pets. |
| 29 | 11 | yes | SpecialDamageAndExplosionsChanceExplosion | There is a %s%% chance of causing an explosion which inflicts %s damage. |
| 29 | 12 | yes | SpecialDamageAndExplosionsChanceExplosionNegated | There is a %s%% chance of causing an explosion which inflicts %s damage. |
| 29 | 21 | yes | SpecialDamageAndExplosionsExplosionCauses | Within a radius of %s fields the explosion causes %s damage. |
| 29 | 22 | yes | SpecialDamageAndExplosionsExplosionCausesNegated | Within a radius of %s fields the explosion causes %s damage. |
| 29 | 31 | yes | SpecialDamageAndExplosionsSurroundingDamage | Causes damage with a chance of %s%% in %s surrounding fields. |
| 29 | 32 | yes | SpecialDamageAndExplosionsSurroundingDamageNegated | Causes damage with a chance of %s%% in %s surrounding fields. |
| 29 | 41 | yes |  | This is not affected by buffs which reduce the cooldown. |
| 29 | 42 | yes |  | This is not affected by buffs which reduce the cooldown. |
| 29 | 51 | yes |  | After %s attacks, the %s effect will be removed. |
| 29 | 52 | yes |  | After %s attacks, the %s effect will be removed. |
| 30 | 11 | yes | SpecialEffects2FocusEnemy | Draws enemy's attention to you. |
| 30 | 12 | yes | SpecialEffects2RemoveEnemyAttention | Removes enemy's attention from you. |
| 30 | 21 | yes | SpecialEffects2TeleportInRadius | Teleports you within a radius of %s fields. |
| 30 | 22 | yes | SpecialEffects2TeleportInRadiusNegated | Teleports you within a radius of %s fields. |
| 30 | 31 | yes | SpecialEffects2MainWeaponCausingChance | With a probability of %s%%, using the main weapon causes %s. |
| 30 | 32 | yes | SpecialEffects2MainWeaponCausingChanceNegated | With a probability of %s%%, using the main weapon causes %s. |
| 30 | 41 | yes | SpecialEffects2SecondaryWeaponCausingChance | With a probability of %s%%, using the secondary weapon causes %s. |
| 30 | 42 | yes | SpecialEffects2SecondaryWeaponCausingChanceNegated | With a probability of %s%%, using the secondary weapon causes %s. |
| 30 | 51 | yes | SpecialEffects2BefriendMonsters | Certain monster groups mistake you as a friend. |
| 30 | 52 | yes | SpecialEffects2BefriendMonstersNegated | Certain monster groups mistake you as a friend. |
| 31 | 11 | yes | CalculatingLevelCalculatedAttackLevel | Calculated Attack Level 0 |
| 31 | 12 | yes | CalculatingLevelCalculatedAttackLevelNegated | Calculated Attack Level 0 |
| 31 | 21 | yes | CalculatingLevelCalculatedDefenceLevel | Calculated Defence Level 0 |
| 31 | 22 | yes | CalculatingLevelCalculatedDefenceLevelNegated | Calculated Defence Level 0 |
| 31 | 31 | yes |  | Melee defence is increased by %s%%. |
| 31 | 32 | yes |  | Melee defence is reduced by %s%%. |
| 31 | 41 | yes |  | Ranged defence is increased by %s%%. |
| 31 | 42 | yes |  | Ranged defence is reduced by %s%%. |
| 31 | 51 | yes |  | Magic defence is increased by %s%%. |
| 31 | 52 | yes |  | Magic defence is reduced by %s%%. |
| 32 | 11 | yes | RecoveryHpRecoveryIncreased | HP recovery is increased by %s. |
| 32 | 12 | yes | RecoveryHpRecoveryDecreased | HP recovery is reduced by %s. |
| 32 | 21 | yes | RecoveryMpRecoveryIncreased | MP recovery is increased by %s. |
| 32 | 22 | yes | RecoveryMpRecoveryDecreased | MP recovery is decreased by %s. |
| 32 | 31 | yes |  | MP recovery is increased by %s%%. |
| 32 | 32 | yes |  | MP recovery is reduced by %s%%. |
| 32 | 41 | yes |  | Increases maximum HP by %s%%. |
| 32 | 42 | yes |  | Decreases maximum HP by %s%%. |
| 32 | 51 | yes |  | Increases maximum MP by %s%%. |
| 32 | 52 | yes |  | Decreases maximum MP by %s%%. |
| 33 | 11 | yes | MaxHpmpMaximumHpIncreased | Maximum HP is increased by %s. |
| 33 | 12 | yes | MaxHpmpMaximumHpDecreased | Maximum HP is decreased by %s. |
| 33 | 21 | yes | MaxHpmpMaximumMpIncreased | Maximum MP is increased by %s. |
| 33 | 22 | yes | MaxHpmpMaximumMpDecreased | Maximum MP is decreased by %s. |
| 33 | 31 | yes | MaxHpmpIncreasesMaximumHp | Increases maximum HP by %s%%. |
| 33 | 32 | yes | MaxHpmpDecreasesMaximumHp | Decreases maximum HP by %s%%. |
| 33 | 41 | yes | MaxHpmpIncreasesMaximumMp | Increases maximum MP by %s%%. |
| 33 | 42 | yes | MaxHpmpDecreasesMaximumMp | Decreases maximum MP by %s%%. |
| 33 | 51 | yes | MaxHpmpMaximumHpmpIncreased | Ancelloan's Apparition. Increases both maximum HP and MP by %s%%. |
| 33 | 52 | yes | MaxHpmpMaximumHpmpDecreased | Ancelloan's Apparition. Reduces both maximum HP and MP by %s%%. |
| 34 | 11 | yes | MultAttackAllAttackIncreased | Increases attack power by a factor of %s. |
| 34 | 12 | yes | MultAttackAllAttackDecreased | Reduces attack power by a factor of %s. |
| 34 | 21 | yes | MultAttackMeleeAttackIncreased | Melee attack power is increased %s times. |
| 34 | 22 | yes | MultAttackMeleeAttackDecreased | Melee attack power is decreased %s times. |
| 34 | 31 | yes | MultAttackRangedAttackIncreased | Ranged attack power is increased %s times. |
| 34 | 32 | yes | MultAttackRangedAttackDecreased | Ranged attack power is decreased %s times. |
| 34 | 41 | yes | MultAttackMagicalAttackIncreased | Magic attack power is increased %s times. |
| 34 | 42 | yes | MultAttackMagicalAttackDecreased | Magic attack power is decreased %s times. |
| 34 | 51 | yes | MultAttackDamageTakenReducedByMissingHp | Damage taken is reduced by (percentage missing HP/%s)%%. |
| 34 | 52 | yes | MultAttackDamageDealtIncreasedByMissingHp | Damage dealt is increased by (percentage missing HP/%s)%%. |
| 35 | 11 | yes | MultDefenceAllDefenceIncreased | Increases defence by a factor of %s. |
| 35 | 12 | yes | MultDefenceAllDefenceDecreased | Reduces defence by a factor of %s. |
| 35 | 21 | yes | MultDefenceMeleeDefenceIncreased | Increases melee defence by a factor of %s. |
| 35 | 22 | yes | MultDefenceMeleeDefenceDecreased | Reduces melee defence by a factor of %s. |
| 35 | 31 | yes | MultDefenceRangedDefenceIncreased | Increases ranged defence by a factor of %s. |
| 35 | 32 | yes | MultDefenceRangedDefenceDecreased | Reduces ranged defence by a factor of %s. |
| 35 | 41 | yes | MultDefenceMagicalDefenceIncreased | Increases magic defence by a factor of %s. |
| 35 | 42 | yes | MultDefenceMagicalDefenceDecreased | Decreases magic defence by a factor of %s. |
| 35 | 51 | yes |  | Damage is reduced by %s%% per debuff stack (max. %s%%). |
| 35 | 52 | yes |  | Damage is increased by %s%% per debuff stack (max. %s%%). |
| 36 | 11 | yes | TimeCircleSkillsGatherEnergy | Gather %s points for the next attack. |
| 36 | 12 | yes | TimeCircleSkillsGatherEnergyNegated | Gather %s points for the next attack. |
| 36 | 21 | yes | TimeCircleSkillsDisableHpConsumption | No HP consumption |
| 36 | 22 | yes | TimeCircleSkillsDisableHpRecovery | No HP recovery |
| 36 | 31 | yes | TimeCircleSkillsDisableMpConsumption | No MP consumption |
| 36 | 32 | yes | TimeCircleSkillsDisableMpRecovery | No MP recovery |
| 36 | 41 | yes | TimeCircleSkillsCancelAllBuff | Cancels all buffs. |
| 36 | 42 | yes | TimeCircleSkillsCancelAllBuffNegated | Cancels all buffs. |
| 36 | 51 | yes | TimeCircleSkillsItemCannotBeUsed | The item cannot be used. |
| 36 | 52 | yes | TimeCircleSkillsItemCannotBeUsedNegated | The item cannot be used. |
| 37 | 11 | yes | RecoveryAndDamagePercentHpRecovered | %s%% HP is recovered. |
| 37 | 12 | yes | RecoveryAndDamagePercentHpReduced | HP is reduced by %s%%. |
| 37 | 21 | yes | RecoveryAndDamagePercentMpRecovered | %s%% MP is recovered. |
| 37 | 22 | yes | RecoveryAndDamagePercentMpReduced | MP is reduced by %s%%. |
| 37 | 31 | yes | RecoveryAndDamagePercentDecreaseEnemyHp | Decreases the opponent's HP by %s%%. |
| 37 | 32 | yes | RecoveryAndDamagePercentDecreaseSelfHp | The damage causes HP to decrease by %s%%. |
| 37 | 41 | yes |  | HP is increased by %s%% per debuff stack (max. %s%%). |
| 37 | 42 | yes |  | HP is reduced by %s%% per debuff stack (max. %s%%). |
| 37 | 51 | yes |  | The enemy's HP is reduced by %s. The caster gains %s%% of this. |
| 37 | 52 | yes |  | The enemy's MP is reduced by (caster level*2). The caster gains %s%% of this. |
| 38 | 11 | yes | CountSummon | Summons %s of %s monsters when MP reaches 0. |
| 38 | 12 | yes | CountSummonChance | With a probability of %s%%, summons %s when MP reaches 0. |
| 38 | 21 | yes |  | Belial reflects your attacks and inflicts damage on your allies. |
| 38 | 22 | yes |  | Belial reflects your attacks and inflicts damage on your allies. |
| 38 | 31 | yes |  | The final damage from critical hits is increased by %s%%. |
| 38 | 32 | yes |  | The damage from critical hits is reduced by %s%%. |
| 38 | 41 | yes |  | When you're defending, the damage from critical hits is increased by %s%%. |
| 38 | 42 | yes |  | When you're defending, the damage from critical hits is reduced by %s%%. |
| 38 | 51 | yes |  | Summons %s NPCs (%s) |
| 38 | 52 | yes |  | Summons %s NPCs (%s) |
| 39 | 11 | yes | NoDefeatAndNoDamageDecreaseHpNoDeath | Decreases HP without dying. |
| 39 | 12 | yes | NoDefeatAndNoDamageDecreaseHpNoKill | Decreases HP without killing. |
| 39 | 21 | yes | NoDefeatAndNoDamageNeverReceiveDamage | Never receives damage. |
| 39 | 22 | yes | NoDefeatAndNoDamageNeverCauseDamage | Never causes damage. |
| 39 | 31 | yes | NoDefeatAndNoDamageTransferAttackPower | Enemy's attack power becomes your own. |
| 39 | 32 | yes | NoDefeatAndNoDamageTransferAttackPowerNegated | Enemy's attack power becomes your own. |
| 39 | 41 | yes |  | Increases maximum HP by %s%%. |
| 39 | 42 | yes |  | Decreases maximum HP by %s%%. |
| 39 | 51 | yes |  | Increases maximum MP by %s%%. |
| 39 | 52 | yes |  | Decreases maximum MP by %s%%. |
| 40 | 11 | yes | SpecialActionsPushBack | Push your opponent back %s fields. |
| 40 | 12 | yes | SpecialActionsPushBackNegated | Push your opponent back %s fields. |
| 40 | 21 | yes | SpecialActionsDrawEnemies | Draws enemies to %s fields away from you. |
| 40 | 22 | yes | SpecialActionsDrawEnemiesNegated | Draws enemies to %s fields away from you. |
| 40 | 31 | yes | SpecialActionsCharge | Charge at enemies within %s fields. |
| 40 | 32 | yes | SpecialActionsChargeNegated | Charge at enemies within %s fields. |
| 40 | 41 | yes | SpecialActionsRunAway | Run away from the enemy. |
| 40 | 42 | yes | SpecialActionsRunAwayNegated | Run away from the enemy. |
| 40 | 51 | yes | SpecialActionsHide | Sneak in |
| 40 | 52 | yes | SpecialActionsSeeHiddenThings | See hidden things |
| 41 | 11 | yes | ModeRange | Range changed to %s. |
| 41 | 12 | yes | ModeReturnRange | Range returned to normal. |
| 41 | 21 | yes | ModeEffectNoDamage | Applies the effect, but causes no damage. |
| 41 | 22 | yes | ModeDirectDamage | Causes direct damage. |
| 41 | 31 | yes | ModeAttackTimeIncreased | Attack time is increased by %s. |
| 41 | 32 | yes | ModeAttackTimeDecreased | Attack time is decreased by %s. |
| 41 | 41 | yes | ModeModeChance | Changes the mode with a probability of %s%%. |
| 41 | 42 | yes | ModeModeChanceNegated | Changes the mode with a probability of %s%%. |
| 41 | 51 | yes | ModeOccuringChance | With a probability of %s%% %s occurs. |
| 41 | 52 | yes | ModeOccuringChanceNegated | With a probability of %s%% %s occurs. |
| 42 | 11 | yes | NoCharacteristicValueAllPowersNullified | All character powers are nullified. |
| 42 | 12 | yes | NoCharacteristicValueAllResistancesNullified | All character resistances are nullified. |
| 42 | 21 | yes | NoCharacteristicValueFireElementNullified | The fire element is nullified. |
| 42 | 22 | yes | NoCharacteristicValueFireResistanceNullified | Fire resistance is nullified. |
| 42 | 31 | yes | NoCharacteristicValueWaterElementNullified | The water element is nullified. |
| 42 | 32 | yes | NoCharacteristicValueWaterResistanceNullified | Water resistance is nullified. |
| 42 | 41 | yes | NoCharacteristicValueLightElementNullified | The light element is nullified. |
| 42 | 42 | yes | NoCharacteristicValueLightResistanceNullified | Light resistance is nullified. |
| 42 | 51 | yes | NoCharacteristicValueDarkElementNullified | The shadow element is nullified. |
| 42 | 52 | yes | NoCharacteristicValueDarkResistanceNullified | Shadow resistance is nullified. |
| 43 | 11 | yes | LightAndShadowInflictDamageToMp | Heal %s%% of inflicted damage by reducing MP. |
| 43 | 12 | yes | LightAndShadowIncreaseMpByAbsorbedDamage | Increases MP by absorbing %s%% of inflicted damage. |
| 43 | 21 | yes | LightAndShadowRemoveBadEffects | Removes all bad effects up to level %s. |
| 43 | 22 | yes | LightAndShadowRemoveGoodEffects | Removes all good effects up to level %s. |
| 43 | 31 | yes | LightAndShadowInflictDamageOnUndead | Inflicts damage on undead with lower level. |
| 43 | 32 | yes | LightAndShadowHealUndead | Heals undead with lower level. |
| 43 | 41 | yes | LightAndShadowAdditionalDamageWhenHidden | Ambush attacks cause %s additional damage. |
| 43 | 42 | yes | LightAndShadowAdditionalDamageOnHiddenEnemy | Attacks on hidden enemy cause %s additional damage. |
| 43 | 51 | yes |  | Ambush attacks cause %s additional damage. |
| 43 | 52 | yes |  | Attacks on hidden enemy cause %s additional damage. |
| 44 | 11 | yes | ItemExpIncreased | Experience gain is increased by %s%%. |
| 44 | 12 | yes | ItemExpIncreasedNegated | Experience gain is increased by %s%%. |
| 44 | 21 | yes | ItemAttackIncreased | All attacks are increased by %s%%. |
| 44 | 22 | yes | ItemDefenceIncreased | All defences are increased by %s%%. |
| 44 | 31 | yes | ItemDropItemsWhenAttacked | For %s rounds, items drop whenever you are attacked. |
| 44 | 32 | yes | ItemDropItemsWhenAttackedNegated | For %s rounds, items drop whenever you are attacked. |
| 44 | 41 | yes | ItemScrollPower | The power of scroll is safe. |
| 44 | 42 | yes | ItemScrollPowerNegated | The power of scroll is safe. |
| 44 | 51 | yes | ItemIncreaseEarnedGold | Increases Gold earned by %s%%. |
| 44 | 52 | yes | ItemIncreaseEarnedGoldNegated | Increases Gold earned by %s%%. |
| 45 | 11 | yes | DebuffResistanceIncreaseBadEffectChance | Below level %s the chance of getting a bad effect is increased by %s%%. |
| 45 | 12 | yes | DebuffResistanceNeverBadEffectChance | Up to level %s there is a %s%% chance of never getting a bad effect. |
| 45 | 21 | yes | DebuffResistanceIncreaseBadGeneralEffectChance | Below level %s the chance of getting a bad general effect is increased by %s%%. |
| 45 | 22 | yes | DebuffResistanceNeverBadGeneralEffectChance | Below level %s there is a %s%% chance of never getting a bad general effect. |
| 45 | 31 | yes | DebuffResistanceIncreaseBadMagicEffectChance | Below level %s the chance of getting a bad magic effect is increased by %s%%. |
| 45 | 32 | yes | DebuffResistanceNeverBadMagicEffectChance | Below level %s there is a %s%% chance of never getting a bad magic effect. |
| 45 | 41 | yes | DebuffResistanceIncreaseBadToxicEffectChance | Below level %s the chance of getting a highly toxic effect is increased by %s%%. |
| 45 | 42 | yes | DebuffResistanceNeverBadToxicEffectChance | Below level %s there is a %s%% chance of never getting a very toxic effect. |
| 45 | 51 | yes | DebuffResistanceIncreaseBadDiseaseEffectChance | Below level %s the chance of getting a disease effect is increased by %s%%. |
| 45 | 52 | yes | DebuffResistanceNeverBadDiseaseEffectChance | Below level %s there is a %s%% chance of never getting a disease effect. |
| 46 | 11 | yes | SpecialBehaviourTeleportRandom | Teleport to a random place on the map. |
| 46 | 12 | yes | SpecialBehaviourTeleportRandomNegated | Teleport to a random place on the map. |
| 46 | 21 | yes | SpecialBehaviourJumpToEveryObject | Jump onto every object within a distance of %s field(s). |
| 46 | 22 | yes | SpecialBehaviourJumpToEveryObjectNegated | Jump onto every object within a distance of %s field(s). |
| 46 | 31 | yes | SpecialBehaviourInflictOnTeam | At a distance of %s, inflict %s on our team. |
| 46 | 32 | yes | SpecialBehaviourInflictOnEnemies | At a distance of %s, inflict %s on our enemies. |
| 46 | 41 | yes | SpecialBehaviourTransformInto | Transforms %s into %s. |
| 46 | 42 | yes | SpecialBehaviourTransformIntoNegated | Transforms %s into %s. |
| 46 | 51 | yes |  | Monsters on general maps will only notice you if you attack them. |
| 46 | 52 | yes |  | Monsters on general maps will only notice you if you attack them. |
| 47 | 11 | yes | QuestSummonMonsterBased | Summons monsters based on the rules. |
| 47 | 12 | yes | QuestSummonMonsterBasedNegated | Summons monsters based on the rules. |
| 47 | 21 | yes | QuestRestoreHpFromDamage | Restores HP equal to %s%% of the damage inflicted (max. %s per attack). |
| 47 | 22 | yes | QuestRestoreMpFromDamage | Restores MP equal to %s%% of the damage inflicted (max. %s per attack). |
| 47 | 31 | yes | QuestSeeHiddenWithinRange | You can see hidden characters within %s spaces of you. |
| 47 | 32 | yes | QuestSeeHiddenWithinRangeNegated | You can see hidden characters within %s spaces of you. |
| 47 | 41 | yes | QuestAdditionalHpPercent | Additional HP is increased by %s%%, but cannot exceed %s%% of max HP. |
| 47 | 42 | yes | QuestAdditionalMpPercent | Additional MP is increased by %s%%, but cannot exceed %s%% of max MP. |
| 47 | 51 | yes | QuestInvisibleBeyondFiveSpaces | Invisible except to enemies within 5 spaces. |
| 47 | 52 | yes | QuestInvisibleBeyondFiveSpacesNegated | Invisible except to enemies within 5 spaces. |
| 48 | 11 | yes | SecondSpCardPlantBomb | Plants a bomb. Activate a second time to detonate the bomb. |
| 48 | 12 | yes | SecondSpCardSetBombWhenAttack | Set up bomb when you attack. |
| 48 | 21 | yes | SecondSpCardPlantSelfDestructionBomb | Plants 3 fire mines. |
| 48 | 22 | yes | SecondSpCardPlantBombWhenAttack | Set up bomb when you attack. |
| 48 | 31 | yes | SecondSpCardReduceEnemySkill | Reduces the skills of all your opponents. |
| 48 | 32 | yes | SecondSpCardReduceEnemySkillNegated | Reduces the skills of all your opponents. |
| 48 | 41 | yes | SecondSpCardCastBuffOnAttacker | The attacker has a %s%% chance of receiving %s. |
| 48 | 42 | yes | SecondSpCardCastBuffOnAttackerNegated | The attacker has a %s%% chance of receiving %s. |
| 48 | 51 | yes | SecondSpCardChanceCausingXBuffOnAttacker | If an opponent attacks you, there is a %s%% chance of %s being inflicted on them. |
| 48 | 52 | yes | SecondSpCardChanceCausingXBuffOnAttackerNegated | If an opponent attacks you, there is a %s%% chance of %s being inflicted on them. |
| 49 | 11 | yes | SpCardUpgradeLowerSpScroll | Use Lower SP Protection Scroll. |
| 49 | 12 | yes | SpCardUpgradeLowerSpScrollNegated | Use Lower SP Protection Scroll. |
| 49 | 21 | yes | SpCardUpgradeHigherSpScroll | Use Higher SP Protection Scroll. |
| 49 | 22 | yes | SpCardUpgradeHigherSpScrollNegated | Use Higher SP Protection Scroll. |
| 49 | 31 | yes | SpCardUpgradeMonsterAndPlayerEffect | There is a 100%% chance for monsters to get [%s] and players [%s]. |
| 49 | 32 | yes | SpCardUpgradeMonsterAndPlayerEffectNegated | There is a 100%% chance for monsters to get [%s] and players [%s]. |
| 49 | 41 | yes | SpCardUpgradeAllSpecialistPointsIncreased | All of your specialist's skill points are increased by %s. |
| 49 | 42 | yes | SpCardUpgradeAllSpecialistPointsIncreasedNegated | All of your specialist's skill points are increased by %s. |
| 49 | 51 | yes | SpCardUpgradePetTrainerExperience | The Pet Trainer specialist gains %s%% more experience. |
| 49 | 52 | yes | SpCardUpgradePetTrainerExperienceNegated | The Pet Trainer specialist gains %s%% more experience. |
| 50 | 11 | yes | HugeSnowmanSnowStorm | A snowstorm. All standing users suffer damage of up to 99%. |
| 50 | 12 | yes | HugeSnowmanSnowStormNegated | A snowstorm. All standing users suffer damage of up to 99%. |
| 50 | 21 | yes | HugeSnowmanEarthQuake | An earthquake. All lying players suffer damage of up to 99%. |
| 50 | 22 | yes | HugeSnowmanEarthQuakeNegated | An earthquake. All lying players suffer damage of up to 99%. |
| 50 | 31 | yes |  | Damage to low-level monsters in the Celestial Lair is increased by %s%%. |
| 50 | 32 | yes |  | Damage from low-level monsters in the Celestial Lair is reduced by %s%%. |
| 50 | 41 | yes |  | Damage to high-level dragons is increased by %s%%. |
| 50 | 42 | yes |  | Damage to high-level dragons is reduced by %s%%. |
| 50 | 51 | yes |  | Decreases HP without dying. |
| 50 | 52 | yes |  | Decreases HP without killing. |
| 51 | 11 | yes | DrainCastDrain | Cast Drain on the enemy. |
| 51 | 12 | yes | DrainCastDrainNegated | Cast Drain on the enemy. |
| 51 | 21 | yes | DrainTransferEnemyHp | The opponent's HP is reduced by %s and the player's HP increased by the same. |
| 51 | 22 | yes | DrainTransferEnemyHpNegated | The opponent's HP is reduced by %s and the player's HP increased by the same. |
| 51 | 31 | yes |  | If you are attacked by the Sun Wolf, this duration has a %s%% chance of increasing by 3 seconds (max. %s times). |
| 51 | 32 | yes |  | If you are attacked by the Sun Wolf, this duration has a %s%% chance of decreasing by 3 seconds (max. %s times). |
| 51 | 41 | yes |  | If you are attacked by the Sun Wolf, there's a %s%% chance %s is cast. |
| 51 | 42 | yes |  | If you are attacked by the Sun Wolf, there's a %s%% chance %s is cast. |
| 51 | 51 | yes |  | If you are attacked by the Sun Wolf, the damage is increased by %s%%. |
| 51 | 52 | yes |  | If you are attacked by the Sun Wolf, the damage is increased by %s%%. |
| 52 | 11 | yes | BossMonstersSkillInflictDamageAfter | Inflicts damage to the player more than %s fields away of %s%%. |
| 52 | 12 | yes | BossMonstersSkillInflictDamageAfterNegated | Inflicts damage to the player more than %s fields away of %s%%. |
| 52 | 21 | yes |  | You gain additional HP equal to %s%% of the Sun Wolf's HP. (Cannot exceed %s%% of your maximum HP.) |
| 52 | 22 | yes |  | You gain additional HP equal to %s%% of the Sun Wolf's HP. (Cannot exceed %s%% of your maximum HP.) |
| 52 | 31 | yes |  | The Sun Wolf and caster have a %s%% chance to receive %s. |
| 52 | 32 | yes |  | The Sun Wolf and caster have a %s%% chance to receive %s. |
| 52 | 41 | yes |  | If the Sun Wolf is already dead, it is resurrected with %s%% HP. |
| 52 | 42 | yes |  | If the Sun Wolf is already dead, it is resurrected with %s%% HP. |
| 52 | 51 | yes |  | If the Sun Wolf has more than %s%% HP, the Sunchaser's attack power increases by %s%%. |
| 52 | 52 | yes |  | If the Sun Wolf has more than %s%% HP, the Sunchaser's attack power increases by %s%%. |
| 53 | 11 | yes | LordHatusInflictDamageAtLocation | Inflict %s%% damage on the players at the set location. |
| 53 | 12 | yes | LordHatusInflictDamageAtLocationNegated | Inflict %s%% damage on the players at the set location. |
| 53 | 21 | yes | LordHatusSunWolfCommandChance | There's a %s%% chance the Sun Wolf receives the command to cast %s on the target. |
| 53 | 22 | yes | LordHatusSunWolfCommandChanceNegated | There's a %s%% chance the Sun Wolf receives the command to cast %s on the target. |
| 53 | 31 | yes | LordHatusRestoreHpFromDamageTaken | Restores HP equal to %s%% of the damage taken (max. %s per attack). |
| 53 | 32 | yes | LordHatusRestoreMpFromDamageTaken | MP is increased by %s%% of damage taken (max. %s per attack). |
| 53 | 41 | yes | LordHatusDamageCap | You cannot deal more than %s damage to enemies. |
| 53 | 42 | yes | LordHatusDamageCapNegated | You cannot deal more than %s damage to enemies. |
| 53 | 51 | yes | LordHatusNamedEffectCriticalChance | Provides a %s%% chance that %s causes a critical hit. |
| 53 | 52 | yes | LordHatusNamedEffectCriticalChanceNegated | Provides a %s%% chance that %s causes a critical hit. |
| 54 | 11 | yes | LordCalvinasInflictDamageAtLocation | Inflict %s%% damage on the players at the set location. |
| 54 | 12 | yes | LordCalvinasInflictDamageAtLocationNegated | Inflict %s%% damage on the players at the set location. |
| 54 | 21 | yes |  | While you're attacking, there is a %s%% chance that a dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 54 | 22 | yes |  | While you're attacking, there is a %s%% chance that a dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 54 | 31 | yes |  | While you're attacking, there is a %s%% chance that a fire dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 54 | 32 | yes |  | While you're attacking, there is a %s%% chance that a fire dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 54 | 41 | yes |  | While you're attacking, there is a %s%% chance that an ice dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 54 | 42 | yes |  | While you're attacking, there is a %s%% chance that an ice dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 54 | 51 | yes |  | While you're attacking, there is a %s%% chance that a moonlight dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 54 | 52 | yes |  | While you're attacking, there is a %s%% chance that a moonlight dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 55 | 11 | yes |  | The effect ends if you take damage from enemies. |
| 55 | 12 | yes | SeSpecialistEnterNumberOfBuffsAndDamage | Enter the number of buffs and the value of the damage (1/1000). |
| 55 | 21 | yes |  |  |
| 55 | 22 | yes | SeSpecialistEnterNumberOfBuffs | Enter the number of buffs. |
| 55 | 31 | yes | SeSpecialistMovingAura | Moving the aura has an effect on the surroundings. |
| 55 | 32 | yes | SeSpecialistDontNeedToEnter | You don't need to enter anything. |
| 55 | 41 | yes | SeSpecialistLowerHpStrongerEffect | The lower your HP, the stronger the effect. |
| 55 | 42 | yes | SeSpecialistDoNotNeedToEnter | You don't need to enter anything. |
| 55 | 51 | yes |  | While you're attacking, there is a %s%% chance that a light dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 55 | 52 | yes |  | While you're attacking, there is a %s%% chance that a light dragon will be summoned to attack your target and deal an extra %s%% of the damage you have already inflicted. |
| 56 | 11 | yes | FourthGlacernonFamilyRaidAllInFieldReceiveDamage | All opponents within %s fields will receive %s%% damage. |
| 56 | 12 | yes | FourthGlacernonFamilyRaidAllInFieldsReceiveDamage | All opponents within %s fields will receive %s%% damage. |
| 56 | 21 | yes |  | Increases HP by %s%% of the current Rage bar. |
| 56 | 22 | yes |  | Increases HP by %s%% of the current Rage bar. |
| 56 | 31 | yes |  | When you use an attack skill, current HP is reduced by %s%%. |
| 56 | 32 | yes |  | When you use an attack skill, current HP is reduced by %s%%. |
| 56 | 41 | yes |  | In PvP all elemental resistances are increased by %s. |
| 56 | 42 | yes |  | In PvP all elemental resistances are reduced by %s. |
| 56 | 51 | yes |  | You are invisible. This state will not change if you are attacked. |
| 56 | 52 | yes |  | You are invisible. This state will not change if you are attacked. |
| 57 | 11 | yes | SummonedMonsterAttackCauseDamage | You cause %s %s%% damage. |
| 57 | 12 | yes | SummonedMonsterAttackCauseDamageNegated | You cause %s %s%% damage. |
| 57 | 21 | yes | SummonedMonsterAttackRemoveDebuffOnHitChance | Upon getting hit, removes one random debuff up to level %s for each attack with a %s%% chance. |
| 57 | 22 | yes | SummonedMonsterAttackRemoveDebuffOnHitChanceNegated | Upon getting hit, removes one random debuff up to level %s for each attack with a %s%% chance. |
| 57 | 31 | yes | SummonedMonsterAttackHpFromDamageByMissingHp | HP is increased by (missing HP/%s)%% of damage given. |
| 57 | 32 | yes | SummonedMonsterAttackHpFromDamageByMissingHpNegated | HP is increased by (missing HP/%s)%% of damage given. |
| 57 | 41 | yes | SummonedMonsterAttackNoHpMpConsumption | No HP/MP consumption |
| 57 | 42 | yes | SummonedMonsterAttackNoHpMpRecovery | No HP/MP recovery |
| 57 | 51 | yes | SummonedMonsterAttackMovementSpeedCapped | Movement speed is increased by %s (max. %s). |
| 57 | 52 | yes | SummonedMonsterAttackMovementSpeedCappedNegated | Movement speed is reduced by %s (max. %s). |
| 58 | 11 | yes | BearSpiritIncreaseMaximumMp | Increases maximum HP by %s%%, however not above 5000 HP. |
| 58 | 12 | yes | BearSpiritDecreaseMaximumMp | Reduces maximum HP by %s%%, however not above 5000 HP. |
| 58 | 21 | yes |  |  |
| 58 | 22 | yes |  |  |
| 58 | 31 | yes | BearSpiritIncreaseMaximumHp | Increases maximum MP by %s%%, however not above 5000 MP. |
| 58 | 32 | yes | BearSpiritDecreaseMaximumHp | Decreases maximum MP by %s%%, however not below 5000 MP. |
| 58 | 41 | yes |  | The damage of the next skill is increased by the damage stored by Soulripper (min. %s, max. %s). |
| 58 | 42 | yes |  | The damage of the next skill is increased by the damage stored by Soulripper (min. %s, max. %s). |
| 58 | 51 | yes |  | If the target is a monster, the damage from all attacks is increased by %s%%. Otherwise the player is forced to transform into the %s. |
| 58 | 52 | yes |  | If the target is a monster, the damage from all attacks is increased by %s%%. Otherwise the player is forced to transform into the %s. |
| 59 | 11 | yes |  |  |
| 59 | 12 | yes |  |  |
| 59 | 21 | yes |  |  |
| 59 | 22 | yes |  |  |
| 59 | 31 | yes | SummonSkillSummon | Summons 1 %s with a probability of %s%%. |
| 59 | 32 | yes | SummonSkillSummonTimed | Summons 1 %s with a probability of %s%%. (Disappears after a set amount of time) |
| 59 | 41 | yes |  | You transform into a powerful brown bear. |
| 59 | 42 | yes |  | You transform back into a druid. |
| 59 | 51 | yes |  | If you have reached Waterfall Frenzy, damage increases by %s%% of the current Rage bar while this in turn drops by %s%%. |
| 59 | 52 | yes |  | If you have reached Waterfall Frenzy, damage increases by %s%% of the current Rage bar while this in turn drops by %s%%. |
| 60 | 11 | yes | InflictSkillInflictDamageAtLocation | Inflict %s%% damage on the players at the set location. |
| 60 | 12 | yes | InflictSkillInflictDamageAtLocationNegated | Inflict %s%% damage on the players at the set location. |
| 60 | 21 | yes |  | The Rage bar increases by %s%%. |
| 60 | 22 | yes |  | The Rage bar sinks by %s%%. |
| 60 | 31 | yes |  | When hit by another character's skill, there is a %s%% chance to reset the cooldown of the skill used. |
| 60 | 32 | yes |  | When hit by another character's skill, there is a %s%% chance to reset the cooldown of the skill used. |
| 60 | 41 | yes |  | Attack power is increased by %s%% of the Rage bar. |
| 60 | 42 | yes |  | Reduces attacks by %s%% of the current Rage bar. |
| 60 | 51 | yes |  | The Rage bar increases by %s%%. |
| 60 | 52 | yes |  | Reduces the Rage bar by %s%%. |
| 61 | 11 | yes |  |  |
| 61 | 12 | yes |  |  |
| 61 | 21 | yes | Type61DebuffDamagePerStackIncreased | Increases damage from ongoing debuffs by %s%% per stack (max. %s%%). |
| 61 | 22 | yes | Type61DebuffDamagePerStackReduced | Reduces damage from ongoing debuffs by %s%% per stack (max. %s%%). |
| 61 | 31 | yes | Type61TransformChance | Provides a %s%% chance to transform into the %s. |
| 61 | 32 | yes | Type61TransformChanceNegated | Provides a %s%% chance to transform into the %s. |
| 61 | 41 | yes | Type61BuffEndsAfterHitCount | If you take damage %s times, %s disappears (excludes damage over time). |
| 61 | 42 | yes | Type61BuffEndsAfterDamageTaken | When you take %s damage, %s disappears (damage over time does not count). |
| 61 | 51 | yes | Type61DamagePerBuffStack | Increases damage by %s per buff stack (max. %s). |
| 61 | 52 | yes | Type61DamagePerDebuffStack | Increases damage by %s per debuff stack (max. %s). |
| 62 | 11 | yes | HideBarrelSkillNoHpConsumption | No HP consumption |
| 62 | 12 | yes | HideBarrelSkillNoHpRecovery | No HP recovery |
| 62 | 21 | yes | HideBarrelSkillDamageByDistanceIncreased | Increases damage by %s%% proportional to distance. |
| 62 | 22 | yes | HideBarrelSkillDamageByDistanceDecreased | Increases damage by %s%% proportional to distance. |
| 62 | 31 | yes | HideBarrelSkillChanceNoDebuffPerStack | Provides a %s%% chance per stack never to receive a debuff up to level %s (max. 30%%). |
| 62 | 32 | yes | HideBarrelSkillChanceDebuffPerStack | Provides a %s%% chance per stack to receive a debuff up to level %s (max. 30%%). |
| 62 | 41 | yes | HideBarrelSkillHpPerBuffStackRestored | Restores %s HP per buff stack (max. %s). |
| 62 | 42 | yes | HideBarrelSkillHpPerBuffStackReduced | Reduces %s HP per buff stack (max. %s). |
| 62 | 51 | yes | HideBarrelSkillOnlyEnemiesWithBuffsAbove | Only applies to enemies with buffs above level %s. |
| 62 | 52 | yes | HideBarrelSkillOnlyEnemiesWithDebuffsAbove | Only applies to enemies with debuffs above level %s. |
| 63 | 11 | yes | FocusEnemyAttentionSkillFocusEnemyAttention | Attracts nearby enemies' attention to you. |
| 63 | 12 | yes |  |  |
| 63 | 21 | yes |  |  |
| 63 | 22 | yes |  |  |
| 63 | 31 | yes |  | Cooking experience gain is increased by %s%%. |
| 63 | 32 | yes |  | Cooking experience gain is reduced by %s%%. |
| 63 | 41 | yes |  | Chance of major success when cooking increased by %s%%. |
| 63 | 42 | yes |  | Chance of major success when cooking reduced by %s%%. |
| 63 | 51 | yes |  | Provides a %s%% chance to receive extra ingredients. |
| 63 | 52 | yes |  | Provides a %s%% chance to receive extra meals. |
| 64 | 11 | yes | TauntSkillReflectsMaximumDamageFrom | Reflects the maximum received damage from %s. |
| 64 | 12 | yes | TauntSkillReflectsMaximumDamageFromNegated | Reflects the maximum received damage from %s. |
| 64 | 21 | yes | TauntSkillDamageInflictedIncreased | The damage inflicted by all attacks will be increased by %s%% with a likelihood of %s%%. Successful defence will recharge energy. |
| 64 | 22 | yes | TauntSkillDamageInflictedDecreased | The damage inflicted by all attacks will be reduced by %s%% with a likelihood of %s%%. Successful defence will recharge energy. |
| 64 | 31 | yes | TauntSkillEffectOnKill | If you are successful in defeating the opponent with this skill, there is a %s%% chance that %s will occur. |
| 64 | 32 | yes | TauntSkillEffectOnKillNegated | If you are successful in defeating the opponent with this skill, there is a %s%% chance that %s will occur. |
| 64 | 41 | yes | TauntSkillTauntWhenKnockdown | If you taunt an opponent that has a Knockdown, there is a %s%% chance that %s will occur. |
| 64 | 42 | yes | TauntSkillTauntWhenNormal | If you taunt an opponent in a normal status, there is a %s%% chance that %s will occur. |
| 64 | 51 | yes | TauntSkillReflectBadEffect | Reflects an opponent's bad effect with a certain likelihood. |
| 64 | 52 | yes | TauntSkillReflectBadEffectNegated | Reflects an opponent's bad effect with a certain likelihood. |
| 65 | 11 | yes | FireCannoneerRangeBuffAoeIncreased | The radius of all area attacks is increased by %s. |
| 65 | 12 | yes | FireCannoneerRangeBuffAoeDecreased | The radius of all area attacks is reduced by %s. |
| 65 | 21 | yes | FireCannoneerRangeBuffFlinch | Flinch from %s of the opponent's blocks. |
| 65 | 22 | yes | FireCannoneerRangeBuffFlinchNegated | Flinch from %s of the opponent's blocks. |
| 65 | 31 | yes |  | If you use 'Teleport to Fishing Spot', you'll be taken to your chosen location. |
| 65 | 32 | yes |  | If you use 'Teleport to Fishing Spot', you'll be taken to your chosen location. |
| 65 | 41 | yes |  | Stores %s%% of the damage caused by this skill (max. %s per target). |
| 65 | 42 | yes |  | Stores %s%% of the damage caused by this skill (max. %s per target). |
| 65 | 51 | yes |  | If you have reached Fury or Frenzy, damage increases by %s%%. |
| 65 | 52 | yes |  | If you have reached Fury or Frenzy, damage increases by %s%%. |
| 66 | 11 | yes | VulcanoElementBuffSkillsIncreased | The fiery skills of the volcano are increased by %s. |
| 66 | 12 | yes | VulcanoElementBuffSkillsDecreased | The fiery skills of the volcano are reduced by %s. |
| 66 | 21 | yes | VulcanoElementBuffReducesEnemyAttack | Reduces the effectiveness of the enemy's attack strengthening buffs. |
| 66 | 22 | yes | VulcanoElementBuffReducesEnemyAttackNegated | Reduces the effectiveness of the enemy's attack strengthening buffs. |
| 66 | 31 | yes | VulcanoElementBuffPullBackBuffIncreasing | If you manage to pull back from the enemy's attack, there is a certain chance the buff will be increased. |
| 66 | 32 | yes | VulcanoElementBuffPullBackBuffIncreasingNegated | If you manage to pull back from the enemy's attack, there is a certain chance the buff will be increased. |
| 66 | 41 | yes | VulcanoElementBuffCriticalDefence | Defence against %s critical damage. |
| 66 | 42 | yes | VulcanoElementBuffCriticalDefenceNegated | Defence against %s critical damage. |
| 66 | 51 | yes |  | Suffers a maximum of %s critical damage when attacked (%s times). |
| 66 | 52 | yes |  | Suffers a maximum of %s critical damage when attacked (%s times). |
| 67 | 11 | yes | DamageConvertingSkillTransferInflictedDamage | Reduces %s%% of the damage that another player within range receives. Instead you take %s%% of that player's damage. |
| 67 | 12 | yes | DamageConvertingSkillTransferInflictedDamageNegated | Reduces %s%% of the damage that another player within range receives. Instead you take %s%% of that player's damage. |
| 67 | 21 | yes | DamageConvertingSkillIncreaseDamageTransfered | Increases the damage transferred from your fellow player by %s%%. |
| 67 | 22 | yes | DamageConvertingSkillDecreaseDamageTransfered | Reduces the damage transferred from your fellow player by %s%%. |
| 67 | 31 | yes | DamageConvertingSkillHpRecoveryIncreased | HP recovery is increased by %s%%. |
| 67 | 32 | yes | DamageConvertingSkillHpRecoveryDecreased | HP recovery is reduced by %s%%. |
| 67 | 41 | yes | DamageConvertingSkillAdditionalDamageCombo | Additional damage received by skill combo: %s%% |
| 67 | 42 | yes | DamageConvertingSkillAdditionalDamageComboNegated | Additional damage received by skill combo: %s%% |
| 67 | 51 | yes | DamageConvertingSkillReflectMaximumReceivedDamage | Reflects the maximum received damage from %s. |
| 67 | 52 | yes | DamageConvertingSkillReflectMaximumReceivedDamageNegated | Reflects the maximum received damage from %s. |
| 68 | 11 | yes | MeditationSkillCausingChance | Has a %s%% probability of causing [%s]. |
| 68 | 12 | yes | MeditationSkillRemovingChance | There is a %s%% chance that %s will be removed. |
| 68 | 21 | yes | MeditationSkillShortMeditation | The short meditation has a %s%% probability of causing %s. |
| 68 | 22 | yes | MeditationSkillShortMeditationNegated | The short meditation has a %s%% probability of causing %s. |
| 68 | 31 | yes | MeditationSkillRegularMeditation | The regular meditation has a %s%% probability of causing %s. |
| 68 | 32 | yes | MeditationSkillRegularMeditationNegated | The regular meditation has a %s%% probability of causing %s. |
| 68 | 41 | yes | MeditationSkillLongMeditation | There is a %s%% chance of the lengthy meditation causing %s. |
| 68 | 42 | yes | MeditationSkillLongMeditationNegated | There is a %s%% chance of the lengthy meditation causing %s. |
| 68 | 51 | yes | MeditationSkillSacrifice | There is a %s%% chance that Sacrifice causes %s. |
| 68 | 52 | yes | MeditationSkillSacrificenegated | There is a %s%% chance that Sacrifice causes %s. |
| 69 | 11 | yes | FalconSkillCausingChanceLocation | There is a %s%% chance that %s will be caused at the set location. |
| 69 | 12 | yes | FalconSkillRemovingChanceLocation | There is a %s%% chance that %s will be removed at the set location. |
| 69 | 21 | yes | FalconSkillHide | Disappear for a short time without a trace. |
| 69 | 22 | yes | FalconSkillHideNegated | Disappear for a short time without a trace. |
| 69 | 31 | yes | FalconSkillAmbush | Carry out a targeted ambush on the opponent. |
| 69 | 32 | yes | FalconSkillAmbushNegated | Carry out a targeted ambush on the opponent. |
| 69 | 41 | yes | FalconSkillFalconFollowing | The enemy is followed and hunted by a falcon and can be attacked from the air. |
| 69 | 42 | yes | FalconSkillFalconFollowingNegated | The enemy is followed and hunted by a falcon and can be attacked from the air. |
| 69 | 51 | yes | FalconSkillFalconFocusLowestHp | Through the falcon's astuteness, the enemy with the lowest HP is identified and attacked. Afterwards this player is marked and can be attacked more intensively by others. |
| 69 | 52 | yes | FalconSkillFalconFocusLowestHpNegated | Through the falcon's astuteness, the enemy with the lowest HP is identified and attacked. Afterwards this player is marked and can be attacked more intensively by others. |
| 70 | 11 | yes | AbsorptionAndPowerSkillAddDamageToHp | Provides a %s%% chance that you receive no damage. In addition, %s%% of the damage you should have received is added to your HP. |
| 70 | 12 | yes | AbsorptionAndPowerSkillRemoveDamnageFromHp | Provides a %s%% chance that you receive no damage. Instead, %s%% of the damage you should have received is deducted from your HP. |
| 70 | 21 | yes |  |  |
| 70 | 22 | yes |  |  |
| 70 | 31 | yes |  |  |
| 70 | 32 | yes |  |  |
| 70 | 41 | yes | AbsorptionAndPowerSkillDamageIncreasedSkill | The damage is increased by %s%% by using the skill %s. |
| 70 | 42 | yes | AbsorptionAndPowerSkillDamageDecreasedSkill | The damage is decreased by %s%% by using the skill %s. |
| 70 | 51 | yes | AbsorptionAndPowerSkillCriticalIncreasedSkill | The critical hit rate is increased by %s%% by using the skill %s. |
| 70 | 52 | yes | AbsorptionAndPowerSkillCriticalDecreasedSkill | The critical hit rate is decreased by %s%% by using the skill %s. |
| 71 | 11 | yes | LeonaPassiveSkillIncreaseDamageAgainst | Increases the damage against %s by %s%%. |
| 71 | 12 | yes | LeonaPassiveSkillDecreaseDamageAgainst | If %s attacks a player, the damage is reduced by %s%%. |
| 71 | 21 | yes | LeonaPassiveSkillIncreaseRecoveryItems | The effectiveness of recovery items is increased by %s%%. |
| 71 | 22 | yes | LeonaPassiveSkillDecreaseRecoveryItems | The effectiveness of recovery items is decreased by %s%%. |
| 71 | 31 | yes | LeonaPassiveSkillOnSpWearCausing | When Leona is wearing the Specialist Partner Card, there is a %s%% chance of causing %s. |
| 71 | 32 | yes | LeonaPassiveSkillOnSpWearRemoving | When Leona is wearing the Specialist Partner Card, there is a %s%% chance that %s will be removed. |
| 71 | 41 | yes | LeonaPassiveSkillDefenceIncreasedInPvp | Reduces damage received in PvP by %s%% (max. 75%%). |
| 71 | 42 | yes | LeonaPassiveSkillDefenceDecreasedInPvp | Defence power in PvP is reduced by %s%%. |
| 71 | 51 | yes | LeonaPassiveSkillAttackIncreasedInPvp | Increases PvP attack power by %s%%. |
| 71 | 52 | yes | LeonaPassiveSkillAttackDecreasedInPvp | Reduces PvP attack power by %s%%. |
| 72 | 11 | yes | FearSkillRestoreRemainingEnemyHp | Restores %s%% of the opponent's remaining HP. |
| 72 | 12 | yes | FearSkillDecreaseRemainingEnemyHp | Decreases %s%% of the opponent's remaining HP. |
| 72 | 21 | yes | FearSkillTimesUsed | If it is used %s times, %s is produced. Can increase to maximum level 2. |
| 72 | 22 | yes | FearSkillTimesUsedNegated | If it is used %s times, %s is produced. Can increase to maximum level 2. |
| 72 | 31 | yes | FearSkillAttackRangedIncreased | The attack range is increased by %s. |
| 72 | 32 | yes | FearSkillAttackRangedDecreased | The attack range is reduced by %s. |
| 72 | 41 | yes | FearSkillMoveAgainstWill | Move in a different direction against the player's will for %s seconds. |
| 72 | 42 | yes | FearSkillMoveAgainstWillNegated | Move in a different direction against the player's will for %s seconds. |
| 72 | 51 | yes | FearSkillProduceWhenAmbushe | There is a %s%% probability of a %s being produced after an ambush attack. |
| 72 | 52 | yes | FearSkillProduceWhenAmbushNegated | There is a %s%% probability of a %s being produced after an ambush attack. |
| 73 | 11 | yes | SniperAttackChanceCausing | After a snipe attack, there is a %s%% chance of causing %s. |
| 73 | 12 | yes | SniperAttackChanceRemoving | After a snipe attack, there is a %s%% chance that %s will be removed. |
| 73 | 21 | yes | SniperAttackAmbushRangeIncreased | The ambush attack range is increased by %s. |
| 73 | 22 | yes | SniperAttackAmbushRangeIncreasedNegated | The ambush attack range is increased by %s. |
| 73 | 31 | yes | SniperAttackProduceChance | There is a %s%% probability of %s being produced after an ambush attack. |
| 73 | 32 | yes | SniperAttackProduceChanceNegated | There is a %s%% probability of %s being produced after an ambush attack. |
| 73 | 41 | yes | SniperAttackKillerHpReducing | If you are killed by another player, their HP is reduced by %s%% of their missing HP. |
| 73 | 42 | yes | SniperAttackKillerHpIncreasing | If you are killed by another player, their HP is increased by %s%% of their missing HP. |
| 73 | 51 | yes | SniperAttackReceiveCriticalFromSniper | You have a %s%% chance to receive a critical hit from a sniper attack. |
| 73 | 52 | yes | SniperAttackReceiveCriticalFromSniperNegated | You have a %s%% chance to receive a critical hit from a sniper attack. |
| 74 | 11 | yes | FrozenDebuffMovementLocked | Lost all body control. Other players can liberate you from the eternal ice. |
| 74 | 12 | yes | FrozenDebuffMovementLockedNegated | Lost all body control. Other players can liberate you from the eternal ice. |
| 74 | 21 | yes |  |  |
| 74 | 22 | yes |  |  |
| 74 | 31 | yes |  | The punishment will be carried out. Death awaits! |
| 74 | 32 | yes |  | The punishment will be carried out. Death awaits! |
| 74 | 41 | yes |  | Depending on your Heat Points, the chance of a critical hit is increased from %s%% to %s%%. |
| 74 | 42 | yes |  | Depending on your Heat Points, the chance of a critical hit is increased from %s%% to %s%%. |
| 74 | 51 | yes |  | All fuel is consumed. For every %s Fuel Points consumed, you recover %s%% HP. |
| 74 | 52 | yes |  | All fuel is consumed. For every %s Fuel Points consumed, you recover %s%% HP. |
| 75 | 11 | yes | JumpBackPushJumpBackChance | There is a %s%% chance of moving back %s field(s). |
| 75 | 12 | yes |  | There is a %s%% chance of moving back %s field(s). |
| 75 | 21 | yes | JumpBackPushPushBackChance | There is a %s%% chance of pushing the enemy away %s field(s). |
| 75 | 22 | yes | JumpBackPushPushBackChanceNegated | Provides a %s%% chance of pushing the enemy away %s field(s) (only in PvP). |
| 75 | 31 | yes | JumpBackPushMeleeDurationIncreased | The effect duration for a melee attack is increased by %s. |
| 75 | 32 | yes | JumpBackPushMeleeDurationDecreased | The effect duration for melee attacks is reduced by %s. |
| 75 | 41 | yes | JumpBackPushRangedDurationIncreased | The effect duration for a ranged attack is increased by %s. |
| 75 | 42 | yes | JumpBackPushRangedDurationDecreased | The effect duration for a ranged attack is reduced by %s. |
| 75 | 51 | yes | JumpBackPushMagicalDurationIncreased | The effect duration for a magic attack is increased by %s. |
| 75 | 52 | yes | JumpBackPushMagicalDurationDecreased | The effect duration for a magic attack is reduced by %s. |
| 76 | 11 | yes | FairyXpIncreaseTeleportToLocation | Teleports you to a selected location. |
| 76 | 12 | yes | FairyXpIncreaseTeleportToLocationNegated | Teleports you to a selected location. |
| 76 | 21 | yes | FairyXpIncreaseIncreaseFairyXpPoints | Your fairy's experience points are increased by %s%%. |
| 76 | 22 | yes | FairyXpIncreaseIncreaseFairyXpPointsNegated | Your fairy's experience points are increased by %s%%. |
| 76 | 31 | yes |  | Provides a %s%% chance spices aren't consumed when cooking. |
| 76 | 32 | yes |  | Provides a %s%% chance extra spices are consumed when cooking. |
| 76 | 41 | yes |  | Increases fullness points by %s. Can only be used when satiety is lower than %s. |
| 76 | 42 | yes |  | Reduces fullness points by %s. Can only be used when satiety is lower than %s. |
| 76 | 51 | yes |  | Fullness points will be increased by %s or reduced by %s. |
| 76 | 52 | yes |  | Provides a %s%% chance to receive %s%% additional fullness points. |
| 77 | 11 | yes | SummonAndRecoverHpChanceSummon | %s%% chance to summon %s. |
| 77 | 12 | yes | SummonAndRecoverHpChanceSummonNegated | %s%% chance to summon %s. |
| 77 | 21 | yes | SummonAndRecoverHpRestoreHp | Restores your HP by %s%%. |
| 77 | 22 | yes | SummonAndRecoverHpReduceHp | Reduces your HP by %s%%. |
| 77 | 31 | yes |  | The Holy Energy bar is filled by %s%%. |
| 77 | 32 | yes |  | The Holy Energy bar is emptied by %s%%. |
| 77 | 41 | yes |  | Regenerates %s%% of the caster's max. HP as HP. |
| 77 | 42 | yes |  | Regenerates %s%% of the caster's max. HP as HP. This effect does not affect the caster. |
| 77 | 51 | yes |  | The damage of ultimate skills is increased by %s%%. |
| 77 | 52 | yes |  | The damage of ultimate skills is increased by %s%%. |
| 78 | 11 | yes | TeamArenaBuffDamageTakenIncreased | There is a %s%% chance of the damage taken in the team arena being increased by %s%%. |
| 78 | 12 | yes | TeamArenaBuffDamageTakenDecreased | There is a %s%% chance of the damage taken in the team arena being reduced by %s%%. |
| 78 | 21 | yes | TeamArenaBuffAttackPowerIncreased | There is a %s%% chance of the attack power being increased by %s%% during an attack in the team arena. |
| 78 | 22 | yes | TeamArenaBuffAttackPowerDecreased | There is a %s%% chance of the attack power being reduced by %s%% during an attack in the team arena. |
| 78 | 31 | yes | TeamArenaBuffSecondBasicAttackChance | When carrying out a basic attack, you have a %s%% chance to execute a second basic attack dealing %s%% damage. |
| 78 | 32 | yes | TeamArenaBuffSecondBasicAttackChanceNegated | When carrying out a basic attack, you have a %s%% chance to execute a second basic attack dealing %s%% damage. |
| 78 | 41 | yes | TeamArenaBuffRestoreHpPercent | Restores %s%% of max. HP (min. %s). |
| 78 | 42 | yes | TeamArenaBuffRestoreMpPercent | Restores %s%% of max. MP (min. %s). |
| 78 | 51 | yes | TeamArenaBuffLowHpDamageReduction | If HP drops below %s%%, incoming damage is reduced by %s%%. |
| 78 | 52 | yes | TeamArenaBuffLowHpAttackBonus | If HP drops below %s%%, all attacks are increased by %s%%. |
| 79 | 11 | yes | ArenaCameraCallParticipant1 | Call Participant Number 1 |
| 79 | 12 | yes |  | Call Participant Number 2 |
| 79 | 21 | yes | ArenaCameraCallParticipant2Negated | Call Participant Number 2 |
| 79 | 22 | yes | ArenaCameraCallParticipant2NegatedNegated | Call Participant Number 2 |
| 79 | 31 | yes | ArenaCameraCallParticipant3 | Call Participant Number 3 |
| 79 | 32 | yes | ArenaCameraCallParticipant3Negated | Call Participant Number 3 |
| 79 | 41 | yes | ArenaCameraSwitchView | Switch from own view to fighting participant. |
| 79 | 42 | yes | ArenaCameraSwitchViewNegated | Switch from own view to fighting participant. |
| 79 | 51 | yes | ArenaCameraSeeHiddenAllies | You can now see hidden characters in your vicinity. |
| 79 | 52 | yes | ArenaCameraSeeHiddenAlliesNegated | You can now see hidden characters in your vicinity. |
| 80 | 11 | yes | DarkCloneSummonSummonDarkCloneChance | There is a %s%% chance that up to %s Dark Clones will be summoned. |
| 80 | 12 | yes | DarkCloneSummonSummonDarkCloneChanceNegated | There is a %s%% chance that up to %s Dark Clones will be summoned. |
| 80 | 21 | yes | DarkCloneSummonConvertRecoveryToDamage | HP replenishing effects will be converted to damage with a probability of %s%%. |
| 80 | 22 | yes | DarkCloneSummonConvertRecoveryToDamageNegated | HP replenishing effects will be converted to damage with a probability of %s%%. |
| 80 | 31 | yes | DarkCloneSummonConvertDamageToHpChance | Transforms the enemy's damage into a healing effect with a %s%% chance of success. The restored HP and collected damage disappear after %s seconds. |
| 80 | 32 | yes | DarkCloneSummonConvertDamageToHpChanceNegated | Transforms the enemy's damage into a healing effect with a %s%% chance of success. The restored HP and collected damage disappear after %s seconds. |
| 80 | 41 | yes | DarkCloneSummonIncreaseEnemyCooldownChance | There is a %s%% chance that the enemy's skill cooldown will be increased by %s%%. If the skill succeeds, your own skill cooldown time will be reduced. |
| 80 | 42 | yes | DarkCloneSummonIncreaseEnemyCooldownChanceNegated | There is a %s%% chance that the enemy's skill cooldown will be increased by %s%%. If the skill succeeds, your own skill cooldown time will be reduced. |
| 80 | 51 | yes | DarkCloneSummonDarkElementDamageIncreaseChance | There is a %s%% chance that damage from the shadow element is increased by %s%% while a shadow fairy is accompanying you. |
| 80 | 52 | yes | DarkCloneSummonDarkElementDamageDecreaseChance | There is a %s%% probability that damage from the shadow element will be decreased by %s%%. |
| 81 | 11 | yes | AbsorbedSpiritApplyEffectIfPresent | There is a probability of %s%% that %s will be applied if an absorbed spirit is present. |
| 81 | 12 | yes | AbsorbedSpiritApplyEffectIfNotPresent | There is a probability of %s%% that %s will be applied if no absorbed spirit is present. |
| 81 | 21 | yes | AbsorbedSpiritResistForcedMovement | Resists forced movement with a probability of %s%%. |
| 81 | 22 | yes | AbsorbedSpiritResistForcedMovementNegated | Resists forced movement with a probability of %s%%. |
| 81 | 31 | yes | AbsorbedSpiritMagicCooldownIncreased | The cooldown time for reusing magic skills is increased by %s%%. |
| 81 | 32 | yes | AbsorbedSpiritMagicCooldownDecreased | The cooldown time for reusing magic skills is reduced by %s%%. |
| 81 | 41 | yes |  | If %s is already active, %s is triggered. |
| 81 | 42 | yes |  | If %s is already active, %s is triggered. |
| 81 | 51 | yes |  | The equipped fairy's element increases by %s if %s is active. |
| 81 | 52 | yes |  | The equipped fairy's element increases by %s if %s is active. |
| 82 | 11 | yes | AngerSkillAttackInRangeNotLocation | Attacks all enemies in visual range. Except at a certain location. |
| 82 | 12 | yes | AngerSkillAttackInRangeNotLocationNegated | Attacks all enemies in visual range. Except at a certain location. |
| 82 | 21 | yes | AngerSkillReduceEnemyHpChance | Reduces the opponent's HP by %s%% with a probability of %s%%. |
| 82 | 22 | yes | AngerSkillReduceEnemyHpByDamageChance | HP is reduced by %s%% of the damage inflicted with a probability of %s%%. |
| 82 | 31 | yes | AngerSkillBlockGoodEffect | There is a %s%% chance that no good effects below level %s can be used. |
| 82 | 32 | yes | AngerSkillBlockGoodEffectNegated | There is a %s%% chance that no good effects below level %s can be used. |
| 82 | 41 | yes | AngerSkillOnlyNormalAttacks | Reduced to carrying out normal attacks by sheer anger. |
| 82 | 42 | yes | AngerSkillOnlyNormalAttacksNegated | Only basic attacks are possible. |
| 82 | 51 | yes |  | Cannot attack for %s sec. |
| 82 | 52 | yes |  | Cannot attack for %s sec. |
| 83 | 11 | yes | MeteoriteTeleportSummonInVisualRange | Summons a Thorn Bush in visual range. |
| 83 | 12 | yes | MeteoriteTeleportSummonInVisualRangeNegated | Summons a Thorn Bush in visual range. |
| 83 | 21 | yes | MeteoriteTeleportTransformTarget | Transforms the target. |
| 83 | 22 | yes | MeteoriteTeleportTransformTargetNegated | Transforms the target. |
| 83 | 31 | yes | MeteoriteTeleportTeleportForward | Quickly teleport %s fields forwards. |
| 83 | 32 | yes | MeteoriteTeleportTeleportForwardNegated | Quickly teleport %s fields forwards. |
| 83 | 41 | yes | MeteoriteTeleportCauseMeteoriteFall | Causes 10 (+%s) meteorites to rain down. |
| 83 | 42 | yes | MeteoriteTeleportCauseMeteoriteFallNegated | Causes 10 (+%s) meteorites to rain down. |
| 83 | 51 | yes | MeteoriteTeleportTeleportYouAndGroupToSavedLocation | Teleports you and %s group members to the saved location. |
| 83 | 52 | yes | MeteoriteTeleportTeleportYouAndGroupToSavedLocationNegated | Teleports you and %s group members to the saved location. |
| 84 | 11 | yes | StealBuffIgnoreDefenceChance | There's a %s%% chance to ignore %s%% of the target's defence. |
| 84 | 12 | yes | StealBuffIgnoreDefenceChanceNegated | There's a %s%% chance to ignore %s%% of the target's defence. |
| 84 | 21 | yes | StealBuffReduceCriticalReceivedChance | There's a %s%% chance of the critical damage received being reduced by %s%%. |
| 84 | 22 | yes | StealBuffReduceCriticalReceivedChanceNegated | There's a %s%% chance of the critical damage received being reduced by %s%%. |
| 84 | 31 | yes | StealBuffChanceSummonOnyxDragon | When attacking, there is a %s%% chance of summoning a shadow clone to carry out an additional attack. |
| 84 | 32 | yes | StealBuffChanceSummonOnyxDragonNegated | When attacking, there is a %s%% chance of summoning a shadow clone to carry out an additional attack. |
| 84 | 41 | yes | StealBuffStealGoodEffect | A successful attack can steal good effects below level %s from the target's partner and transfer them to allies within 3 fields. Max. %s effects can be stolen. |
| 84 | 42 | yes | StealBuffStealGoodEffectNegated | A successful attack can steal good effects below level %s from the target's partner and transfer them to allies within 3 fields. Max. %s effects can be stolen. |
| 84 | 51 | yes |  | There's a %s%% chance that the transformation will be aborted. Cannot be attacked for %s sec. if successfully aborted. The target then returns to their previous state. |
| 84 | 52 | yes |  | There's a %s%% chance that the transformation will be aborted. Cannot be attacked for %s sec. if successfully aborted. The target then returns to their previous state. |
| 85 | 11 | yes | Type85PositionSwapper | Swaps places with the target. Once the effect ends, the character returns to their initial position. |
| 85 | 12 | yes |  | Swaps places with the target. Once the effect ends, the character returns to their initial position. |
| 85 | 21 | yes | Type85OnDeathIncreaseRepLost | When you die, the amount of reputation you lose is increased by %s%%. |
| 85 | 22 | yes | Type85OnDeathReduceRepLost | When you die, the amount of reputation you lose is reduced by %s%%. |
| 85 | 31 | yes | Type85OnDeathIncreaseHxpLost | When you die, the amount of champion level experience you lose is increased by %s%%. |
| 85 | 32 | yes | Type85OnDeathReduceHxpLost | When you die, the amount of champion level experience you lose is reduced by %s%%. |
| 85 | 41 | yes | Type85IncreaseDamageVsAngels | Increases damage against players of the Angel faction by %s%%. |
| 85 | 42 | yes | Type85ReduceDamageVsAngels | When attacking angels, your max. damage decreases to %s%%. |
| 85 | 51 | yes | Type85IncreaseDamageVsDeamons | Increases damage against players of the Demon faction by %s%%. |
| 85 | 52 | yes | Type85ReduceDamageVsDeamons | When attacking demons, your max. damage decreases to %s%%. |
| 86 | 11 | yes | Type86CdResetWithProb | Provides a %s%% chance to reset the cooldown of the attack skill used. |
| 86 | 12 | yes | Type86CdResetWithProbUncapped | Provides a %s%% chance to reset the cooldown of the attack skill used. |
| 86 | 21 | yes | Type86IncreaseGrovyBeachVibesProb | There is a %s%% chance to apply %s to the alliance within 5 fields. |
| 86 | 22 | yes | Type86DecreaseGrovyBeachVibesProb | There is a %s%% chance to remove %s from the alliance within 5 fields. |
| 86 | 31 | yes | Type86IncreaseDamageVsHigherLevelMonster | Provides a %s%% chance of increasing damage by %s%% if the monster has the same or a higher combat level than the character. |
| 86 | 32 | yes | Type86DecreaseDamageVsHigherLevelMonster | Provides a %s%% chance of reducing damage by %s%% if the monster has a higher level than the character. |
| 86 | 41 | yes | Type86BlockAmountOfBadEffect | Blocks %s bad effects up to level %s. |
| 86 | 42 | yes |  | Blocks %s bad effects up to level %s. |
| 86 | 51 | yes | Type86ContagiousDebuffAroundCell | There is a %s%% chance of an ally within 2 spaces receiving %s. |
| 86 | 52 | yes |  | There is a %s%% chance of removing %s from an ally within 2 spaces. |
| 87 | 11 | yes | Type87IncreaseFameRecived | Increases fame received by %s%%. |
| 87 | 12 | yes | Type87DecreaseFameRecived | Reduces fame received by %s%%. |
| 87 | 21 | yes | Type87IncreaseHxpRecived | Increases champion experience received by %s%%. |
| 87 | 22 | yes | Type87DecreaseHxpRecived | Reduces champion experience received by %s%%. |
| 87 | 31 | yes | Type87ReciveRandomItem | After %s hour(s) you can receive %s unknown items while you are being accompanied. |
| 87 | 32 | yes |  | After %s hour(s) you can receive %s unknown items while you are being accompanied. |
| 87 | 41 | yes | Type87RemoveBuffFromEnemy | There's a %s%% chance to remove a buff from the enemy. |
| 87 | 42 | yes |  | There's a %s%% chance to remove a buff from the enemy. |
| 87 | 51 | yes | Type87Unknow | Uses lich magic to create a Bone Drake and give the caster Dragon Vitality. |
| 87 | 52 | yes |  | Uses lich magic to create a Bone Drake and give the caster Strong Dragon Vitality. |
| 88 | 11 | yes | Type88MateReturnToMiniland | Return to Miniland and regenerate %s%% of your HP. If you have maximum HP, you receive %s%% additional HP. |
| 88 | 12 | yes |  |  |
| 88 | 21 | yes | Type88IncreaseDamageVsHiden | Attacks on hidden enemies cause %s additional damage. |
| 88 | 22 | yes | Type88DecreaseDamageVsHiden |  |
| 88 | 31 | yes | Type88IncreaseDamageFromHiden | Damage from hidden enemies is increased by %s%%. |
| 88 | 32 | yes | Type88DecreaseDamageFromHiden | Damage from hidden enemies is reduced by %s%%. |
| 88 | 41 | yes | Type88RevealsHidenEnemy | Reveals hidden enemies within %s spaces. |
| 88 | 42 | yes |  |  |
| 88 | 51 | yes | Type88SkillResetXTimes | Skill can be used %s times without cooldown. |
| 88 | 52 | yes |  |  |
| 89 | 11 | yes | Type89Transform | Transform and assume the Dragon Stance with which you can carry out flame attacks. |
| 89 | 12 | yes | Type89RemoveTransform | Transform and assume the Haetae Stance with which you can carry out attacks using the power of the beast. |
| 89 | 21 | yes | Type89Unknow | Provides a %s%% probability to reset the cooldown of %s. |
| 89 | 22 | yes |  | Provides a %s%% probability to reset the cooldown of %s. |
| 89 | 31 | yes | Type89NegateGoodEffect | There is a %s%% chance that no good effects below level %s can be used. |
| 89 | 32 | yes |  | There is a %s%% chance that no good effects below level %s can be used. |
| 89 | 41 | yes | Type89NormalAndJobExpIncreased | Combat and job experience points earned are increased by %s%%. |
| 89 | 42 | yes |  | Combat and job experience points earned are increased by %s%%. |
| 89 | 51 | yes | Type89ZhephyrMagicalArrow | Provides a %s%% chance of firing a magical arrow when you use magic or ranged attack skills. |
| 89 | 52 | yes |  | Provides a %s%% chance of firing a magical arrow when you use attack skills. |
| 90 | 00 | no | Type90DamageVsSealedAndReduceFireDamage |  |
| 90 | 11 | yes |  | Provides a %s%% chance to increase fire attack damage by %s%%. |
| 90 | 12 | yes |  | Provides a %s%% chance to reduce fire attack damage by %s%%. |
| 90 | 21 | yes | Type90IncreaseDamageVsSealed | Damage against monsters in Sealed Vessels is increased by %s%% and against monsters in the Land of Death by %s%%. |
| 90 | 22 | yes | Type90DecreaseDamageVsSealed | Damage against monsters in Sealed Vessels is reduced by %s%% and against monsters in the Land of Death by %s%%. |
| 90 | 31 | yes | Type90RainbowBattleDamageAndSpeedBoost | PvP attack power is increased by %s%% during the Rainbow Battle. Movement speed is increased by %s during the Rainbow Battle. |
| 90 | 32 | yes |  | PvP attack power is reduced by %s%% during the Rainbow Battle. Movement speed is reduced by %s during the Rainbow Battle. |
| 90 | 41 | yes | Type90IncreaseDamageVsSealedAndGlacernonMonsters | Increases attack power by %s%% when fighting monsters from Sealed Vessels and by %s%% when fighting monsters in Glacernon. |
| 90 | 42 | yes | Type90DecreaseDamageVsSealedAndGlacernonMonsters | Reduces attack power by %s%% when fighting monsters from Sealed Vessels and by %s%% when fighting monsters in Glacernon. |
| 90 | 51 | yes |  | If the target has less than %s%% HP, your Holy Energy bar fills by %s%% (does not apply to NosMates). |
| 91 | 11 | yes | Type91AddBuff | You dodge attacks and have a %s%% chance to cause %s. |
| 91 | 12 | yes |  | You dodge attacks and have a %s%% chance to cause %s. |
| 91 | 21 | yes | Type91BuffSkillBooster | Use a buff skill while Enlightenment is active to receive additional effects. |
| 91 | 22 | yes |  |  |
| 91 | 31 | yes | Type91AllowFullMoonSkill | Allows you to use Full Moon skills. |
| 91 | 32 | yes |  |  |
| 91 | 41 | yes | Type91AllowLotusPowerSkill | Allows you to use Lotus Flower skills. |
| 91 | 42 | yes |  |  |
| 91 | 51 | yes | Type91ReduceCriticalDamageRecived | The next damage you inflict on a marked enemy will be increased by %s%% and consume the Mark of the Moon. |
| 91 | 52 | yes | Type91IncreaseCriticalDamageRecived | The next damage inflicted by the enemy will be reduced by %s%% and consume the Mark of the Moon. |
| 92 | 11 | yes | Type92AddBuffOnReceiveAttack | Whenever you are attacked, you have a %s%% chance of generating %s. |
| 92 | 12 | yes |  | Whenever you are attacked, you have a %s%% chance of generating %s. |
| 92 | 21 | yes | Type92ChanceToReciveOpportunityToAttack | After using an attack skill, there is a chance of receiving another Opportunity to Attack. |
| 92 | 22 | yes |  | After using an attack skill, there is a chance of receiving another Opportunity to Attack. |
| 92 | 31 | yes |  | The next damage you inflict on a marked enemy will be increased by %s%% and consume the Mark of the Full Moon. |
| 92 | 32 | yes |  | The next damage you inflict on a marked enemy will be increased by %s%% and consume the Mark of the Full Moon. |
| 92 | 41 | yes | Type92TransformEnemyDebuff | If the effect Bound by Moonlight is active on your opponent, you will trigger Bound by the Full Moon's Light. |
| 92 | 42 | yes |  | If the effect Bound by Moonlight is active on your opponent, you will trigger Bound by the Full Moon's Light. |
| 92 | 51 | yes |  | Can only be used with a buff of %s or higher. |
| 92 | 52 | yes |  | You cannot use skills that increase Heat Points. |
| 93 | 11 | yes | ElementPercentageIncreaseFireElement | Fire element bonus damage is increased by %s%%. |
| 93 | 12 | yes | ElementPercentageDecreaseFireElement | Fire element is reduced by %s%%. |
| 93 | 21 | yes | ElementPercentageIncreaseWaterElement | Water element is increased by %s%%. |
| 93 | 22 | yes | ElementPercentageDecreaseWaterElement | Water element is reduced by %s%%. |
| 93 | 31 | yes | ElementPercentageIncreaseLightElement | Light element is increased by %s%%. |
| 93 | 32 | yes | ElementPercentageDecreaseLightElement | Light element is reduced by %s%%. |
| 93 | 41 | yes | ElementPercentageIncreaseShadowElement | Shadow element is increased by %s%%. |
| 93 | 42 | yes | ElementPercentageDecreaseShadowElement | Shadow element is reduced by %s%%. |
| 93 | 51 | yes | ElementPercentageIncreaseAllElements | All elements are increased by %s%%. |
| 93 | 52 | yes | ElementPercentageDecreaseAllElements | All elements are reduced by %s%%. |
| 94 | 11 | yes | ElementResistancePercentageIncreaseAllElementsRes | All elemental resistances are increased by %s%%. |
| 94 | 12 | yes | ElementResistancePercentageDecreaseAllElementsRes | All elemental resistances are reduced by %s%%. |
| 94 | 21 | yes | ElementResistancePercentageIncreaseFireRes | Fire resistance is increased by %s%%. |
| 94 | 22 | yes | ElementResistancePercentageDecreaseFireRes | Fire resistance is reduced by %s%%. |
| 94 | 31 | yes | ElementResistancePercentageIncreaseWaterRes | Water resistance is increased by %s%%. |
| 94 | 32 | yes | ElementResistancePercentageDecreaseWaterRes | Water resistance is reduced by %s%%. |
| 94 | 41 | yes | ElementResistancePercentageIncreaseLightRes | Light resistance is increased by %s%%. |
| 94 | 42 | yes | ElementResistancePercentageDecreaseLightRes | Light resistance is reduced by %s%%. |
| 94 | 51 | yes | ElementResistancePercentageIncreaseDarkRes | Shadow resistance is increased by %s%%. |
| 94 | 52 | yes | ElementResistancePercentageDecreaseDarkRes | Shadow resistance is reduced by %s%%. |
| 95 | 11 | yes | Type95DropX2Chance | When a hunted monster drops an item, there's a %s%% chance that it drops the item a second time. This effect is only triggered when the dropped item is assigned to the player, not when it is shared in the group. |
| 95 | 12 | yes | Type95NoDropChance | When hunting monsters, there is a %s%% chance they will not drop any loot. |
| 95 | 21 | yes | Type95ChanceCausingXBuffOnEnemyOnAttack | On attack, there is a %s%% chance of inflicting %s on your opponent. |
| 95 | 22 | yes | Type95ChanceCausingXBuffOnEnemyOnAttackNegated | On attack, there is a %s%% chance of inflicting %s on your opponent. |
| 95 | 31 | yes | Type95ChanceReceivingXBuffOnAttack | On attack, there is a %s%% chance of receiving %s. |
| 95 | 32 | yes | Type95ChanceReceivingXBuffOnAttackNegated | On attack, there is a %s%% chance of receiving %s. |
| 95 | 41 | yes | Type95ChanceCausingXBuffOnEnemyOnDefence | When you're defending, there is a %s%% chance of inflicting %s on your opponent. |
| 95 | 42 | yes | Type95ChanceCausingXBuffOnEnemyOnDefenceNegated | When you're defending, there is a %s%% chance of inflicting %s on your opponent. |
| 95 | 51 | yes | Type95ChanceReceivingXBuffOnDefence | When you're defending, there is a %s%% chance of receiving %s. |
| 95 | 52 | yes | Type95ChanceReceivingXBuffOnDefenceNegated | When you're defending, there is a %s%% chance of receiving %s. |
| 96 | 11 | yes | Type96IncreaseFairyElement | Increases the equipped fairy's element by %s. |
| 96 | 12 | yes | Type96DecreaseFairyElement | The equipped fairy's element decreases by %s. |
| 96 | 21 | yes | Type96IncreaseFairyElementOnAttack | On attack there is a %s%% chance of increasing your equipped fairy's element by %s. |
| 96 | 22 | yes | Type96DecreaseFairyElementOnAttack | On attack there is a %s%% chance of decreasing your equipped fairy's element by %s. |
| 96 | 31 | yes | Type96IncreaseDamageToMonsters | Damage to monsters is increased by %s%%. |
| 96 | 32 | yes | Type96DecreaseDamageToMonsters | Damage to monsters is reduced by %s%%. |
| 96 | 41 | yes |  | Provides a %s%% chance when catching a fish to catch an extra fish. |
| 96 | 42 | yes |  | Provides a %s%% chance when catching a fish to catch an extra fish. |
| 96 | 51 | yes |  | The probability of catching a rare fish is increased by %s%%. |
| 96 | 52 | yes |  | The probability of catching a rare fish is increased by %s%%. |
| 97 | 11 | yes | Type97AddBuffWithMissingHpChance | Provides a (current HP/max. HP * %s)%% chance to inflict %s on your opponent. |
| 97 | 12 | yes |  | Provides a (missing HP/max. HP * %s)%% chance to inflict %s on your opponent. |
| 97 | 21 | yes | Type97IncreaseUltimatePoins | If the attack is successful, you earn %s ultimate points. |
| 97 | 22 | yes | Type97DecreaseUltimatePoints | If you are attacked while blocking, you earn %s ultimate points. |
| 97 | 31 | yes | Type97AllowUltimateSkills | You can use ultimate skills. |
| 97 | 32 | yes |  | You can use ultimate skills. |
| 97 | 41 | yes | Type97DarkElementAddBuff | When you're attacked with a shadow element skill, there is a %s%% chance of triggering %s. |
| 97 | 42 | yes |  | When you're attacked with a shadow element skill, there is a %s%% chance of triggering %s. |
| 97 | 51 | yes | Type97IncreaseAttackAndDefenceForDebuff | When debuffs are applied to you, attack and defence power are increased by %s%% per debuff. |
| 97 | 52 | yes | Type97DecreaseAttackAndDefenceForDebuff | When buffs are applied to you, attack and defence power are increased by %s%% per buff. |
| 98 | 11 | yes | Type98AllElementalDamageIncreasedChance | With a %s%% probability all elemental damage is increased by %s%%. |
| 98 | 12 | yes | Type98AllElementalDamageReducedChance | With a %s%% probability all elemental damage is reduced by %s%%. |
| 98 | 21 | yes | Type98FireDamageIncreasedChance | With a %s%% probability fire attack damage is increased by %s%%. |
| 98 | 22 | yes | Type98FireDamageReducedChance | With a %s%% probability fire attack damage is reduced by %s%%. |
| 98 | 31 | yes | Type98WaterDamageIncreasedChance | With a %s%% probability water attack damage is increased by %s%%. |
| 98 | 32 | yes | Type98WaterDamageReducedChance | With a %s%% probability water attack damage is reduced by %s%%. |
| 98 | 41 | yes | Type98LightDamageIncreasedChance | With a %s%% probability light attack damage is increased by %s%%. |
| 98 | 42 | yes | Type98LightDamageReducedChance | With a %s%% probability light attack damage is reduced by %s%%. |
| 98 | 51 | yes | Type98ShadowDamageIncreasedChance | With a %s%% probability shadow attack damage is increased by %s%%. |
| 98 | 52 | yes | Type98ShadowDamageReducedChance | With a %s%% probability shadow attack damage is reduced by %s%%. |
| 99 | 11 | yes | Type99IncreaseDamageToLowLevelPlant | Damage to low-level plants is increased by %s%%. |
| 99 | 12 | yes | Type99DecreaseDamageToLowLevelPlant | Damage to low-level plants is reduced by %s%%. |
| 99 | 21 | yes | Type99IncreaseDamageToLowLevelAnimal | Damage to low-level animals is increased by %s%%. |
| 99 | 22 | yes | Type99DecreaseDamageToLowLevelAnimal | Damage to low-level animals is reduced by %s%%. |
| 99 | 31 | yes | Type99IncreaseDamageToLowLevelMonster | Damage to low-level monsters is increased by %s%%. |
| 99 | 32 | yes | Type99DecreaseDamageToLowLevelMonster | Damage to low-level monsters is reduced by %s%%. |
| 99 | 41 | yes | Type99IncreaseDamageToKovolt | Damage to Kovolts is increased by %s%%. |
| 99 | 42 | yes | Type99DecreaseDamageToKovolt | Damage to Kovolts is reduced by %s%%. |
| 99 | 51 | yes | Type99IncreaseDamageToCatsie | Damage to Catsies is increased by %s%%. |
| 99 | 52 | yes | Type99DecreaseDamageToCatsie | Damage to Catsies is reduced by %s%%. |
| 100 | 11 | yes | Type100IncreaseDamageToLowLevelSpirit | Damage to low-level spirits is increased by %s%%. |
| 100 | 12 | yes | Type100DecreaseDamageToLowLevelSpirit | Damage to low-level spirits is reduced by %s%%. |
| 100 | 21 | yes | Type100IncreaseDamageToAngel | Damage to angels is increased by %s%%. |
| 100 | 22 | yes | Type100DecreaseDamageToAngel | Damage to angels is reduced by %s%%. |
| 100 | 31 | yes | Type100IncreaseDamageToDemon | Damage to demons is increased by %s%%. |
| 100 | 32 | yes | Type100DecreaseDamageToDemon | Damage to demons is reduced by %s%%. |
| 100 | 41 | yes | Type100IncreaseDamageToLowLevelUndead | Damage to low-level undead is increased by %s%%. |
| 100 | 42 | yes | Type100DecreaseDamageToLowLevelUndead | Damage to low-level undead is reduced by %s%%. |
| 100 | 51 | yes | Type100IncreaseProductionPointConsume | Production point consumption is reduced by %s%%. |
| 100 | 52 | yes | Type100DecreaseProductionPointConsume | Production point consumption is increased by %s%%. |
| 101 | 11 | yes | Type101IncreaseDamageToLODMonster | Damage to monsters in the Land of Death is increased by %s%%. |
| 101 | 12 | yes | Type101DecreaseDamageToLODMonster | Damage to monsters in the Land of Death is reduced by %s%%. |
| 101 | 21 | yes | Type101IncreaseDamageToSealed | Damage to monsters from Sealed Vessels is increased by %s%%. |
| 101 | 22 | yes | Type101DecreaseDamageToSealed | Damage to monsters from Sealed Vessels is reduced by %s%%. |
| 101 | 31 | yes | Type101IncreaseXpGain | Experience gain is increased by %s%%. |
| 101 | 32 | yes | Type101DecreaseXpGain | Experience gain is reduced by %s%%. |
| 101 | 41 | yes | Type101IncreaseJXpGain | Job experience gain is increased by %s%%. |
| 101 | 42 | yes | Type101DecreaseJXpGain | Job experience gain is reduced by %s%%. |
| 101 | 51 | yes | Type101IncreaseDodge | Dodge is increased by %s%%. |
| 101 | 52 | yes | Type101DecreaseDodge | Dodge is reduced by %s%%. |
| 102 | 11 | yes | Type102IncreaseSLDamage | Your specialist's attack skill points are increased by %s. |
| 102 | 12 | yes | Type102DecreaseSLDamage | Your specialist's attack skill points are reduced by %s. |
| 102 | 21 | yes | Type102IncreaseSLDefence | Your specialist's defence skill points are increased by %s. |
| 102 | 22 | yes | Type102DecreaseSLDefence | Your specialist's defence skill points are reduced by %s. |
| 102 | 31 | yes | Type102IncreaseSLElement | Your specialist's elemental skill points are increased by %s. |
| 102 | 32 | yes | Type102DecreaseSLElement | Your specialist's elemental skill points are reduced by %s. |
| 102 | 41 | yes | Type102IncreaseSLHp | Your specialist's HP/MP skill points are increased by %s. |
| 102 | 42 | yes | Type102DecreaseSLHp | Your specialist's HP/MP skill points are reduced by %s. |
| 102 | 51 | yes | Type102IncreaseHitRate | Hit rate is increased by %s%%. |
| 102 | 52 | yes | Type102DecreaseHitRate | Hit rate is reduced by %s%%. |
| 103 | 11 | yes | Type103IncreaseAllAttack | All attacks are increased by %s%%. |
| 103 | 12 | yes | Type103DecreaseAllAttack | All attacks are reduced by %s%%. |
| 103 | 21 | yes | Type103IncreaseMeleeAttack | Melee attacks are increased by %s%%. |
| 103 | 22 | yes | Type103DecreaseMeleeAttack | Melee attacks are reduced by %s%%. |
| 103 | 31 | yes | Type103IncreaseRangedAttack | Ranged attacks are increased by %s%%. |
| 103 | 32 | yes | Type103DecreaseRangedAttack | Ranged attacks are reduced by %s%%. |
| 103 | 41 | yes | Type103IncreaseMagicAttack | Magic attacks are increased by %s%%. |
| 103 | 42 | yes | Type103DecreaseMagicAttack | Magic attacks are reduced by %s%%. |
| 103 | 51 | yes | Type103IncreaseConcentration | Concentration is increased by %s%%. |
| 103 | 52 | yes | Type103DecreaseConcentration | Concentration is reduced by %s%%. |
| 104 | 11 | yes | Type104ReflectOnDeff | When you're defending, there is a %s%% chance of %s%% of the damage being reflected at the enemy (up to 50%% of the max. HP of the player with the buff). |
| 104 | 12 | yes | Type104ReflectOnDeffNegated | When you're defending, there is a %s%% chance of %s%% of the damage being reflected at the opponent. |
| 104 | 21 | yes | Type104AreaDamageEachSecond | All opponents within %s space(s) take %s damage every 1.5 seconds. |
| 104 | 22 | yes | Type104AreaDamageEachSecondNegated | All opponents within %s space(s) take %s damage every 1.5 seconds. |
| 104 | 31 | yes | Type104SummonMonsterOnDef | When you're defending, there is a %s%% chance of summoning a(n) %s. |
| 104 | 32 | yes | Type104SummonTwoMonstersOnDef | When you're defending, there is a %s%% chance of summoning two %s. |
| 104 | 41 | yes | Type104MateAttackIncreased | Increases the attack power of your NosMate by %s%%. |
| 104 | 42 | yes | Type104MateAttackIncreasedNegated | Increases the attack power of your NosMate by %s%%. |
| 104 | 51 | yes | Type104AreaBuffEachSecond | All opponents within %s space(s) suffer %s every 1.5 seconds. |
| 104 | 52 | yes | Type104AreaBuffOnAlliesEachSecond | Allies within %s space(s) suffer [%s] every 1.5 seconds. |
| 105 | 11 | yes | Type105ApocalypsePowerChance | When you're attacking, there is a %s%% chance (caster level * 18) of an opponent suffering damage from Apocalypse Power. They will also receive the corresponding debuff. |
| 105 | 12 | yes | Type105ApocalypsePowerChanceNegated | When you're attacking, there is a %s%% chance (caster level * 18) of an opponent suffering damage from Apocalypse Power. They will also receive the corresponding debuff. |
| 105 | 21 | yes | Type105ReflectionPowerChance | When you're attacking, there is a %s%% chance of receiving Reflection Power. |
| 105 | 22 | yes | Type105ReflectionPowerChanceNegated | When you're attacking, there is a %s%% chance of receiving Reflection Power. |
| 105 | 31 | yes | Type105WolfPowerChance | When you're attacking, there is a %s%% chance (caster level * 15) of an opponent suffering damage from Wolf Power. They will also receive the corresponding debuff. |
| 105 | 32 | yes | Type105WolfPowerChanceNegated | When you're attacking, there is a %s%% chance (caster level * 15) of an opponent suffering damage from Wolf Power. They will also receive the corresponding debuff. |
| 105 | 41 | yes | Type105KnockBackOnAttackChance | When you're attacking, there is a %s%% chance of the opponent being knocked back 4 spaces (only in PvP). |
| 105 | 42 | yes | Type105KnockBackOnDefenceChance | When you're defending, there is a %s%% chance of the opponent being knocked back 4 spaces (only in PvP). |
| 105 | 51 | yes | Type105ExplosionPowerChance | When you attack, there is a %s%% chance (caster level * 17) of an opponent suffering damage from Explosion Power. They will also receive the corresponding debuff. |
| 105 | 52 | yes | Type105ExplosionPowerChanceNegated | When you attack, there is a %s%% chance (caster level * 17) of an opponent suffering damage from Explosion Power. They will also receive the corresponding debuff. |
| 106 | 11 | yes | Type106AgilityPowerChance | When you're attacking, there is a %s%% chance of receiving Agility Power. |
| 106 | 12 | yes | Type106AgilityPowerChanceNegated | When you're attacking, there is a %s%% chance of receiving Agility Power. |
| 106 | 21 | yes | Type106LightningPowerChance | When you're attacking, there is a %s%% chance (caster level * 18) of an opponent suffering damage from Lightning Power. They will also receive the corresponding debuff. |
| 106 | 22 | yes | Type106LightningPowerChanceNegated | When you're attacking, there is a %s%% chance (caster level * 18) of an opponent suffering damage from Lightning Power. They will also receive the corresponding debuff. |
| 106 | 31 | yes | Type106CursePowerChance | When you're attacking, there is a %s%% chance of triggering Curse Power on the opponent. |
| 106 | 32 | yes | Type106CursePowerChanceNegated | When you're attacking, there is a %s%% chance of triggering Curse Power on the opponent. |
| 106 | 41 | yes | Type106BearPowerChance | When you're attacking, there is a %s%% chance (caster level * 23) of an opponent suffering damage from Bear Power. They will also receive the corresponding debuff. |
| 106 | 42 | yes | Type106BearPowerChanceNegated | When you're attacking, there is a %s%% chance (caster level * 23) of an opponent suffering damage from Bear Power. They will also receive the corresponding debuff. |
| 106 | 51 | yes | Type106FrostPowerChance | When you're attacking, there is a %s%% chance of receiving Frost Power. |
| 106 | 52 | yes | Type106FrostPowerChanceNegated | When you're attacking, there is a %s%% chance of receiving Frost Power. |
| 107 | 11 | yes | Type107MagicArmourFlat | Magic armour prevents %s damage. |
| 107 | 12 | yes | Type107MagicArmourFlatNegated | Magic armour prevents %s damage. |
| 107 | 21 | yes | Type107ElementResistanceOnDefenceChance | When you're defending, there is a %s%% chance of your resistance to the element with which the opponent is attacking increasing by %s. |
| 107 | 22 | yes | Type107ElementResistanceOnDefenceChanceNegated | When you're defending, there is a %s%% chance of your resistance to the element with which the opponent is attacking decreasing by %s. |
| 107 | 31 | yes | Type107MaxAdditionalHpIncreased | The maximum additional HP increases by %s%%. |
| 107 | 32 | yes | Type107MaxAdditionalHpDecreased | The maximum additional HP decreases by %s%%. |
| 107 | 41 | yes | Type107IgnoreBlockChance | When your attack is blocked, there's a %s%% chance of ignoring the block. |
| 107 | 42 | yes | Type107IgnoreBlockChanceNegated | When your attack is blocked, there's a %s%% chance of ignoring the block. |
| 107 | 51 | yes | Type107DodgeIncreasedPercent | Dodge is increased by %s%%. |
| 107 | 52 | yes | Type107HitRateIncreasedPercent | Hit rate of all attacks is increased by %s%%. |
| 108 | 11 | yes | Type108ReflectCritOnDef | When you receive a critical hit, there is a %s%% chance of %s%% being reflected to the opponent. |
| 108 | 12 | yes | Type108ReflectCritOnDefNegated | When you receive a critical hit, there is a %s%% chance of %s%% being reflected to the opponent. |
| 108 | 21 | yes | Type108IncreaseDodgeByMissingHp | You have a (percent of HP missing * %s/100)%% probability of dodging attacks. |
| 108 | 22 | no | Type108DecreaseDodgeByMissingHp |  |
| 108 | 31 | yes | Type108IncreaseCritDamageWithCount | The final critical hit damage is increased by %s%% per critical hit (max. %s hits). |
| 108 | 32 | yes | Type108DecreaseCritDamageWithCount | The final critical hit damage is increased by %s%% per critical hit (max. %s hits). |
| 108 | 41 | yes | Type108TransferDamageToMp | When you're defending, there is a %s%% chance of %s%% of the damage being deducted from your MP instead of your HP. |
| 108 | 42 | yes | Type108TransferDamageToMpNegated | When you're defending, there is a %s%% chance of %s%% of the damage being deducted from your MP instead of your HP. |
| 108 | 51 | yes | Type108IncreaseAttackByMagicRes | Overall attack power is increased by %s%% of magic defence. |
| 108 | 52 | yes | Type108DecreaseAttackByMagicRes | Overall attack power is increased by %s%% of magic defence. |
| 109 | 11 | yes | Type109LionLoaDamageChance | When attacking, there is a %s%% chance of %s%% additional damage being added by the lion loa. |
| 109 | 12 | yes | Type109LionLoaDamageChanceNegated | When attacking, there is a %s%% chance of %s%% additional damage being added by the lion loa. |
| 109 | 21 | yes | Type109EagleLoaDamageChance | When attacking, there is a %s%% chance of %s%% additional damage being added by the eagle loa. |
| 109 | 22 | yes | Type109EagleLoaDamageChanceNegated | When attacking, there is a %s%% chance of %s%% additional damage being added by the eagle loa. |
| 109 | 31 | yes | Type109SnakeLoaDamageChance | When attacking, there is a %s%% chance of %s%% additional damage being added by the snake loa. |
| 109 | 32 | yes | Type109SnakeLoaDamageChanceNegated | When attacking, there is a %s%% chance of %s%% additional damage being added by the snake loa. |
| 109 | 41 | yes | Type109BearLoaDamageChance | When attacking, there is a %s%% chance of %s%% additional damage being added by the bear loa. |
| 109 | 42 | yes | Type109BearLoaDamageChanceNegated | When attacking, there is a %s%% chance of %s%% additional damage being added by the bear loa. |
| 109 | 51 | yes | Type109IncreaseElementOnDebuff | When you receive a debuff, there is a %s%% chance of every element being increased by %s%%. |
| 109 | 52 | yes | Type109DecreaseElementOnDebuff | When you receive a debuff, there is a %s%% chance of every element being increased by %s%%. |
| 110 | 11 | yes | Type110RecoveryHpOnDodge | Your HP increases by %s whenever you dodge. |
| 110 | 12 | yes | Type110DecreaseHpOnDodge | Your HP increases by %s whenever you dodge. |
| 110 | 21 | yes | Type110BeastKingTarget | The Beast King has chosen you as the target of his bite attack. |
| 110 | 22 | yes |  | The Beast King has chosen you as the target of his bite attack. |
| 110 | 31 | yes |  | Maximum HP is increased by %s%%. |
| 110 | 32 | yes |  | Maximum HP is decreased by %s%%. |
| 110 | 41 | yes |  | Maximum MP is increased by %s%%. |
| 110 | 42 | yes |  | Maximum MP is decreased by %s%%. |
| 110 | 51 | yes |  | Your HP increases by %s%% of missing HP when you dodge attacks. |
| 111 | 11 | yes | MateSynergyMateHpRestoredOnMonsterKill | If you or your NosMates defeat a monster, any NosMates who are currently active will be restored %s%% HP. |
| 111 | 12 | yes | MateSynergyMateHpRestoredOnMonsterKillNegated | If you or your NosMates defeat a monster, any NosMates who are currently active will be restored %s%% HP. |
| 111 | 21 | yes | MateSynergyOwnerHpRestoredOnMateDefeat | If one of your NosMates is defeated, you will be restored %s%% HP. |
| 111 | 22 | yes | MateSynergyMateHpRestoredOnMateDefeat | If one of your NosMates is defeated, any NosMates who are currently active will be restored %s%% HP. |
| 111 | 31 | yes | MateSynergyBuffOnSelfOnMonsterKillChance | If you defeat a monster, there is a %s%% chance of %s being cast on you. |
| 111 | 32 | yes | MateSynergyBuffOnMatesOnMonsterKillChance | If you defeat a monster, there is a %s%% chance of %s being cast on your NosMates. |
| 111 | 41 | yes | MateSynergyMateAttackOnMonsterKillChance | If you defeat a monster, there is a %s%% chance of your NosMates' attack power increasing by %s%%. |
| 111 | 42 | yes | MateSynergyMateDefenceOnMonsterKillChance | If you defeat a monster, there is a %s%% chance that all the defences of your NosMates will increase by %s%%. |
| 111 | 51 | yes | MateSynergyAllMatesHpRestoredOnMonsterKillChance | If you defeat a monster, there is a %s%% chance of all your NosMates being restored %s%% HP. |
| 111 | 52 | yes | MateSynergyAllMatesHpRestoredOnMonsterKillChanceNegated | If you defeat a monster, there is a %s%% chance of all your NosMates being restored %s%% HP. |
| 112 | 11 | yes | MateAndSquadAttackOnMateMonsterKill | If your NosMates defeat a monster, your attack power will be increased by %s%%. |
| 112 | 12 | yes | MateAndSquadDefenceOnMateMonsterKill | If your NosMates defeat a monster, your defences will be increased by %s%%. |
| 112 | 21 | yes | MateAndSquadDebuffEnemiesOnDeath | If you're defeated, all opponents within %s spaces receive %s. |
| 112 | 22 | yes | MateAndSquadBuffMatesOnDeath | If you're defeated, all of your NosMates within %s spaces receive %s. |
| 112 | 31 | yes | MateAndSquadBuffOnSelfWhenAllMatesDefeatedChance | If all of your NosMates are defeated, there is a %s%% chance of %s being cast on you. |
| 112 | 32 | yes | MateAndSquadBuffOnSelfWhenAllMatesDefeatedChanceNegated | If all of your NosMates are defeated, there is a %s%% chance of %s being cast on you. |
| 112 | 41 | yes | MateAndSquadBuffMatesInRangeOnMonsterKill | If you defeat a monster, %s will be cast on any of your NosMates within %s spaces. |
| 112 | 42 | yes | MateAndSquadBuffMatesInRangeOnMonsterKillNegated | If you defeat a monster, %s will be cast on any of your NosMates within %s spaces. |
| 112 | 51 | yes | MateAndSquadSummonSquad | Summons Squad %s. |
| 112 | 52 | yes | MateAndSquadSummonSquadNegated | Summons Squad %s. |
| 113 | 11 | yes | SquadAndGravitySquadAttackIncreased | Your squads' attack power is increased (%s + level x %s). |
| 113 | 12 | yes | SquadAndGravitySquadDefenceIncreased | Your squads' defence is increased (%s + level x %s). |
| 113 | 21 | yes | SquadAndGravityMateBuffChance | Your NosMates have a %s%% chance to receive %s. |
| 113 | 22 | yes | SquadAndGravityMateBuffChanceNegated | Your NosMates have a %s%% chance to receive %s. |
| 113 | 31 | yes | SquadAndGravityGravPointsReducedPerBasicAttack | Your Grav Points reduce by %s each time you perform a basic attack. |
| 113 | 32 | yes | SquadAndGravityAntiGravPointsReducedPerBasicAttack | Your Anti-Grav Points reduce by %s each time you perform a basic attack. |
| 113 | 41 | yes | SquadAndGravityGravitationSkillsDisabled | All your gravitation skills are unavailable. |
| 113 | 42 | yes | SquadAndGravityAntiGravitationSkillsDisabled | All your anti-gravitation skills are unavailable. |
| 113 | 51 | yes | SquadAndGravityGravPointsReduced | Grav Points are reduced by %s. |
| 113 | 52 | yes | SquadAndGravityAntiGravPointsReduced | Anti-Grav Points are reduced by %s. |
| 114 | 11 | yes | Type114FireDamageIncreasedChance | If you are attacked with the fire element, there's a %s%% chance the damage will be increased by %s%%. |
| 114 | 12 | yes | Type114FireDamageReducedChance | If you are attacked with the fire element, there's a %s%% chance the damage will be reduced by %s%%. |
| 114 | 21 | yes | Type114WaterDamageIncreasedChance | If you are attacked with the water element, there's a %s%% chance the damage will be increased by %s%%. |
| 114 | 22 | yes | Type114WaterDamageReducedChance | If you are attacked with the water element, there's a %s%% chance the damage will be reduced by %s%%. |
| 114 | 31 | yes | Type114LightDamageIncreasedChance | If you are attacked with the light element, there's a %s%% chance the damage will be increased by %s%%. |
| 114 | 32 | yes | Type114LightDamageReducedChance | If you are attacked with the light element, there's a %s%% chance the damage will be reduced by %s%%. |
| 114 | 41 | yes | Type114ShadowDamageIncreasedChance | If you are attacked with the shadow element, there's a %s%% chance the damage will be increased by %s%%. |
| 114 | 42 | yes | Type114ShadowDamageReducedChance | If you are attacked with the shadow element, there's a %s%% chance the damage will be reduced by %s%%. |
| 114 | 51 | yes | Type114EnemySoftCritDamageReduced | Reduces enemies' soft crit damage by %s%%. |
| 114 | 52 | yes | Type114EnemySoftCritDamageReducedNegated | Reduces enemies' soft crit damage by %s%%. |
| 115 | 11 | yes | FishingCatchChanceIncreased | The probability of catching a fish is increased by %s%%. |
| 115 | 12 | yes | FishingCatchChanceReduced | The probability of catching a fish is reduced by %s%%. |
| 115 | 21 | yes | FishingLineBreakChanceIncreased | The probability of your fishing line breaking is increased by %s%%. |
| 115 | 22 | yes | FishingLineBreakChanceReduced | The probability of your fishing line breaking is reduced by %s%%. |
| 115 | 31 | yes | FishingBaitNotConsumedChance | There's a %s%% chance of the bait not being consumed when you cast your fishing line. |
| 115 | 32 | yes | FishingBaitNotConsumedChanceNegated | There's a %s%% chance of the bait not being consumed when you cast your fishing line. |
| 115 | 41 | yes | FishingFishSizeIncreasedChance | There's a %s%% chance that the size of the fish caught increases by %s%%. |
| 115 | 42 | yes | FishingFishSizeReducedChance | There's a %s%% chance that the size of the fish caught decreases by %s%%. |
| 115 | 51 | yes | FishingFishingExperienceIncreased | The fishing experience points you receive are increased by %s%%. |
| 115 | 52 | yes | FishingFishingExperienceReduced | The fishing experience points you receive are reduced by %s%%. |
| 116 | 11 | yes | Type116CleansingArmourPowerChance | %s%% chance to trigger Power of Cleansing Armour when defending. |
| 116 | 12 | yes | Type116CleansingArmourPowerChanceNegated | %s%% chance to trigger Power of Cleansing Armour when defending. |
| 116 | 21 | yes | Type116RegenerationPowerChance | %s%% chance to trigger Power of Regeneration when defending. |
| 116 | 22 | yes | Type116RegenerationPowerChanceNegated | %s%% chance to trigger Power of Regeneration when defending. |
| 116 | 31 | yes | Type116FlamePowerChance | %s%% chance to trigger Power of the Flame when defending. |
| 116 | 32 | yes | Type116FlamePowerChanceNegated | %s%% chance to trigger Power of the Flame when defending. |
| 116 | 41 | yes | Type116PurityPowerChance | %s%% chance to trigger Power of Purity when defending. |
| 116 | 42 | yes | Type116PurityPowerChanceNegated | %s%% chance to trigger Power of Purity when defending. |
| 116 | 51 | yes | Type116ResistancePowerChance | %s%% chance to trigger Power of Resistance when defending. |
| 116 | 52 | yes | Type116ResistancePowerChanceNegated | %s%% chance to trigger Power of Resistance when defending. |
| 117 | 11 | yes | Type117BloodPowerChance | %s%% chance to trigger Power of Blood when defending. |
| 117 | 12 | yes | Type117BloodPowerChanceNegated | %s%% chance to trigger Power of Blood when defending. |
| 117 | 21 | yes | Type117ConversionPowerChance | %s%% chance to trigger Power of Conversion when defending. |
| 117 | 22 | yes | Type117ConversionPowerChanceNegated | %s%% chance to trigger Power of Conversion when defending. |
| 117 | 31 | yes | Type117UnyieldingPowerChance | %s%% chance to trigger Power of Unyielding when defending. |
| 117 | 32 | yes | Type117UnyieldingPowerChanceNegated | %s%% chance to trigger Power of Unyielding when defending. |
| 117 | 41 | yes | Type117InstinctPowerChance | %s%% chance to trigger Power of Instinct when defending. |
| 117 | 42 | yes | Type117InstinctPowerChanceNegated | %s%% chance to trigger Power of Instinct when defending. |
| 117 | 51 | yes | Type117HealingPowerChance | %s%% chance to trigger Power of Healing when defending. |
| 117 | 52 | yes | Type117HealingPowerChanceNegated | %s%% chance to trigger Power of Healing when defending. |
| 118 | 11 | yes | ArmourPiercingPierceArmour | Pierces armour. Increases damage of Pinpoint attacks. |
| 118 | 12 | yes |  | Pierces armour thoroughly. Significantly increases damage of Pinpoint attacks. |
| 118 | 21 | yes | ArmourPiercingCountsAsPinpoint | Counts as a Pinpoint attack. Damage against targets with pierced armour is increased by %s%%. |
| 118 | 22 | yes |  | Counts as a Pinpoint attack. Damage against targets with pierced armour is increased by %s%%. |
| 118 | 31 | yes | ArmourPiercingCriticalDamageWhileActive | Increases critical damage by %s%% if %s is active. |
| 118 | 32 | yes |  | Reduces critical damage by %s%% if %s is active. |
| 118 | 41 | yes | ArmourPiercingCriticalChanceWhileActive | Increases the chance of inflicting a critical hit by %s%% if %s is active. |
| 118 | 42 | yes |  | Reduces the chance of inflicting a critical hit by %s%% if %s is active. |
| 118 | 51 | yes | ArmourPiercingOnBlockWhileActive | You receive %s when you block while %s is active. |
| 118 | 52 | yes |  | The target receives [%s] when you attack while [%s] is active. |
| 119 | 11 | yes | HeatPointsAttackByHeatPoints | Depending on your Heat Points, your attack power is increased from %s%% to %s%%. |
| 119 | 12 | yes | HeatPointsAttackByHeatPointsNegated | Depending on your Heat Points, your attack power is increased from %s%% to %s%%. |
| 119 | 21 | yes | HeatPointsAttackRangeByHeatPoints | Depending on your Heat Points, your attack range is increased from %s to %s. |
| 119 | 22 | yes | HeatPointsAttackRangeByHeatPointsNegated | Depending on your Heat Points, your attack range is increased from %s to %s. |
| 119 | 31 | yes | HeatPointsAttackRadiusByHeatPoints | Depending on your Heat Points, your attack radius is increased from %s%% to %s%%. |
| 119 | 32 | yes | HeatPointsAttackRadiusByHeatPointsNegated | Depending on your Heat Points, your attack radius is increased from %s%% to %s%%. |
| 119 | 41 | yes | HeatPointsPullEnemiesOnHighHeating | If the High Heating effect is active, enemies from %s spaces around the target are pulled together in one place. |
| 119 | 42 | yes | HeatPointsPullEnemies | Pulls enemies from %s spaces around the target together in one place. |
| 119 | 51 | yes | HeatPointsEnemyHpReducedByDamageTakenOnHighHeating | If the High Heating effect or higher is active, the opponent's HP is reduced by %s%% of the damage you take. |
| 119 | 52 | yes | HeatPointsEnemyHpReducedByDamageTakenOnHighHeatingNegated | If the High Heating effect or higher is active, the opponent's HP is reduced by %s%% of the damage you take. |
| 120 | 11 | yes | HeatAndGravityConsumeHeatForDamage | All Heat Points are consumed. The damage increases by %s%% of the consumed Heat Points. |
| 120 | 12 | yes |  | All Heat Points are consumed. The damage increases by %s%% of the consumed Heat Points. |
| 120 | 21 | yes | HeatAndGravityGainSharpness | You receive %s Sharpness Points. |
| 120 | 22 | yes |  | You lose %s Sharpness Points. |
| 120 | 31 | yes | HeatAndGravityGainGravity | You receive %s Grav Points and lose %s Antigrav Points. |
| 120 | 32 | yes |  | You receive %s Anti-Grav Points and lose %s Grav Points. |
| 120 | 41 | yes | HeatAndGravityGravitationDamage | The damage of Gravitation skills is increased by %s%%. |
| 120 | 42 | yes |  | The damage of Anti-Gravitation skills is increased by %s%%. |
| 120 | 51 | yes | HeatAndGravityDamageFromGravityPool | Increases damage by (current number of Grav and Anti-Grav Points) * %s%%. All Grav and Anti-Grav Points are consumed. |
| 120 | 52 | yes |  | Increases damage by (current number of Grav and Anti-Grav Points) * %s%%. All Grav and Anti-Grav Points are consumed. |
| 121 | 11 | yes | Type121DamageIncreasedWhileBuffActive | Increases damage by %s%% if %s is active. |
| 121 | 12 | yes | Type121DamageReducedWhileBuffActive | Increases damage by %s%% if %s is active. |
| 121 | 21 | yes | Type121MpCostIncreasedWhileBuffActive | MP consumption of skills (including magic) is increased by %s%% if %s is active. |
| 121 | 22 | yes | Type121MpCostReducedWhileBuffActive | MP consumption of skills (including magic) is reduced by %s%% if %s is active. |
| 121 | 31 | yes | Type121WaterElementWhileBuffActive | Water element is increased by %s%% if %s is active. |
| 121 | 32 | yes | Type121AllResistancesWhileBuffActive | All elemental resistances are increased by %s%% if %s is active. |
| 121 | 41 | yes | Type121MaxMpSpentForFuelPoints | You lose %s%% of your max. MP and receive %s Fuel Points. |
| 121 | 42 | yes | Type121MaxMpSpentForFuelPointsNegated | You lose %s%% of your max. MP and receive %s Fuel Points. |
| 121 | 51 | yes | Type121FuelPointsSpentForDamage | You consume %s Fuel Points. Your damage is increased by %s%%. |
| 121 | 52 | yes | Type121FuelPointsSpentForDamageNegated | You consume %s Fuel Points. Your damage is increased by %s%%. |
| 122 | 11 | yes | FuelPointsConsumeForShadow | You consume %s Fuel Points. Your shadow element is increased by %s%%. |
| 122 | 12 | yes |  | You consume %s Fuel Points. Your shadow element is increased by %s%%. |
| 122 | 21 | yes | FuelPointsConsumeForEffect | You consume %s Fuel Points and receive the %s effect. |
| 122 | 22 | yes |  | You lose %s Fuel Points and inflict %s to the target. |
| 122 | 31 | yes | FuelPointsConsumeForCooldownReset | You consume %s Fuel Points. There's a %s%% chance to reset the cooldown of the used skill. |
| 122 | 32 | yes |  | You consume %s Fuel Points. There's a %s%% chance to reset the cooldown of the used skill. |
| 122 | 41 | yes | FuelPointsConsumeForDefencePierce | You consume %s Fuel Points and ignore %s%% of the opponent's defence. |
| 122 | 42 | yes |  | You consume %s Fuel Points and ignore %s%% of the opponent's defence. |
| 122 | 51 | yes | FuelPointsGainHeat | You receive %s Heat Points. |
| 122 | 52 | yes |  | You lose %s Heat Points. |
| 123 | 11 | yes | Type123TameStarPetChance | Tames %s-star pets with a %s%% chance. |
| 123 | 12 | yes | Type123TameStarPetChanceNegated | Tames %s-star pets with a %s%% chance. |
| 123 | 21 | yes | Type123DebuffEndsAfterPlayerHits | This debuff disappears when you get hit %sx by other players. |
| 123 | 22 | yes | Type123DebuffEndsAfterPlayerHitsNegated | This debuff disappears when you get hit %sx by other players. |
| 123 | 31 | yes | Type123UserHpRecovered | %s%% of the user's HP are recovered. |
| 123 | 32 | yes | Type123UserHpRecoveredNegated | %s%% of the user's HP are recovered. |
| 123 | 41 | yes | Type123DiesOnAnyAction | You die as soon as you move, attack or use a skill. |
| 123 | 42 | yes | Type123DiesOnAnyActionNegated | You die as soon as you move, attack or use a skill. |
| 123 | 51 | yes | Type123Act9MonsterDamageIncreased | Increases damage to all monsters in Act 9 (excluding raids) and the Act 9 Land of Life by %s%%. |
| 123 | 52 | yes | Type123Act9MonsterDamageTakenReduced | Reduces damage from all monsters in Act 9 (excluding raids) and the Act 9 Land of Life by %s%%. |
| 124 | 11 | yes | TokenSystemRequiresTokens | Can only be cast if you have %s tokens. |
| 124 | 12 | yes |  | Can only be cast if you have %s tokens. |
| 124 | 21 | yes | TokenSystemChanceToGainTokens | Provides a %s%% chance to receive %s token(s). |
| 124 | 22 | yes |  | Provides a %s%% chance to receive %s token(s). |
| 124 | 31 | yes | TokenSystemEnhancementActive | Token enhancement buff active. |
| 124 | 32 | yes |  | Token enhancement buff active. |
| 124 | 41 | yes | TokenSystemGaugeIncrease | Token gauge increased by %s. |
| 124 | 42 | yes |  | Token gauge reduced by %s. |
| 124 | 51 | yes | TokenSystemSpendForGuaranteedReward | If you spend %s tokens, you are guaranteed to receive %s. |
| 124 | 52 | yes |  | If you spend %s tokens, you are guaranteed to deal %s to the target. |
| 125 | 11 | yes | Type125AttackForSpentTokens | If you spend %s tokens, your attack power increases by %s%%. |
| 125 | 12 | yes | Type125SpecialistElementForSpentTokens | If you spend %s tokens, your specialist's original element increases by %s%%. |
| 125 | 21 | yes | Type125AttackForMineralTokens | If you have %s Mineral tokens, your attack power is increased by %s%%. |
| 125 | 22 | yes | Type125DefenceForMineralTokens | If you have %s Mineral tokens, your defence power is increased by %s%%. |
| 125 | 31 | yes | Type125GaugeIncreasedOnDamageTaken | When you take damage, the gauge increases by %s (max. %s times). |
| 125 | 32 | yes | Type125GaugeIncreasedOnDamageTakenNegated | When you take damage, the gauge increases by %s (max. %s times). |
| 125 | 41 | yes | Type125InflictOnEnemyOnAttackChance | On attack there is a %s%% chance of inflicting %s on your enemy. |
| 125 | 42 | yes | Type125TriggerOnAttackChance | On attack there is a %s%% chance of triggering %s. |
| 125 | 51 | yes | Type125AttackForExplosiveChargeTokens | If you have %s Explosive Charge tokens, your attack power is increased by %s%%. |
| 125 | 52 | yes | Type125DodgeForExplosiveChargeTokens | If you have %s Explosive Charge tokens, your dodge is increased by %s%%. |
| 126 | 11 | yes | Type126TriggerOnSelfAndOwnerChance | Has a %s%% probability of triggering %s on itself and its owner. |
| 126 | 12 | yes | Type126RemoveFromSelfAndOwnerChance | Has a %s%% probability of removing %s from itself and its owner. |
| 126 | 21 | yes | Type126DamageFromMainWeaponAttack | When you cast an attack skill, the damage dealt is increased by %s%% of your main weapon's attack power. |
| 126 | 22 | yes | Type126DamageFromSecondaryWeaponAttack | When you cast an attack skill, the damage dealt is increased by %s%% of your secondary weapon's attack power. |
| 126 | 31 | yes | Type126ExtraRaidTokenChance | Provides a %s%% chance to gain an additional raid token (excluding hardcore raid tokens). |
| 126 | 32 | yes | Type126ExtraRaidTokenIncludingHardcoreChance | There is a %s%% chance that you'll get an additional raid token (including hardcore raid tokens). |
| 126 | 41 | yes | Type126ExtraRaidBoxChance | There is a %s%% chance that you'll get an additional raid box. |
| 126 | 42 | yes | Type126ExtraRaidBoxChanceNegated | There is a %s%% chance that you'll get an additional raid box. |
| 126 | 51 | yes | Type126AttackForPyrosphereTokens | If you have %s Pyrosphere tokens, your attack power is increased by %s%%. |
| 126 | 52 | yes | Type126FireElementForPyrosphereTokens | If you have %s Pyrosphere tokens, your fire element is increased by %s%%. |
| 127 | 11 | yes | Type127NamedRaidAndHardcoreAttack | All attacks in the %s raid and hardcore raid are increased by %s%%. |
| 127 | 12 | yes | Type127NamedRaidAndHardcoreDefence | All defences in the %s raid and hardcore raid are increased by %s%%. |
| 127 | 21 | yes | Type127AllHardcoreRaidsAttack | All attacks in all hardcore raids are increased by %s%%. |
| 127 | 22 | yes | Type127AllHardcoreRaidsDefence | All defences in all hardcore raids are increased by %s%%. |
| 127 | 31 | yes | Type127AttackForSpareBatteryTokens | If you have %s Spare Battery tokens, your attack power is increased by %s%%. |
| 127 | 32 | yes | Type127HitRateForSpareBatteryTokens | If you have %s Spare Battery tokens, your hit rate is increased by %s%%. |
| 127 | 41 | yes | Type127AttackInSealedVesselsAndGlacernon | Increases attack power by %s%% when fighting monsters from Sealed Vessels and by %s%% when fighting normal monsters in Glacernon. |
| 127 | 42 | yes | Type127AttackReducedInSealedVesselsAndGlacernon | Reduces attack power by %s%% when fighting monsters from Sealed Vessels and by %s%% when fighting normal monsters in Glacernon. |
| 127 | 51 | yes | Type127GlacernonPvpAttackIncreased | Increases PvP attack power in Glacernon by %s%%. |
| 127 | 52 | yes | Type127GlacernonPvpAttackReduced | Reduces PvP attack power in Glacernon by %s%%. |
| 128 | 11 | yes | Type128GlacernonPvpDamageIncreased | Increases damage in PvP in Glacernon by %s%%. |
| 128 | 12 | yes | Type128GlacernonPvpDamageTakenReduced | Reduces damage taken in PvP in Glacernon by %s%%. |
| 128 | 21 | yes | Type128CelestialSpireCatacombsAttack | All attacks in the Celestial Spire Catacombs are increased by %s%%. |
| 128 | 22 | yes | Type128CelestialSpireCatacombsDefence | All defences in the Celestial Spire Catacombs are increased by %s%%. |
| 128 | 31 | yes | Type128Act10MonsterDamageIncreased | Increases damage to all monsters in Act 10 (excluding raids) by %s%%. |
| 128 | 32 | yes | Type128Act10MonsterDamageTakenReduced | Reduces damage received from all monsters in Act 10 (excluding raids) by %s%%. |
| 128 | 41 | yes | Type128GuaranteedBuffOnOwnerOnly | Provides a 100%% chance to receive %s. This effect is only applied if you (not your pet) has it. |
| 128 | 42 | yes | Type128GuaranteedBuff | Provides a 100%% chance to receive %s. |
| 128 | 51 | yes | Type128DodgeChanceLimitedUses | Provides a %s%% chance to dodge an enemy attack (max. %s times). |
| 128 | 52 | yes | Type128NextAttackHitsChanceLimitedUses | Provides a %s%% chance that your next attack hits (max. %s times). |
| 129 | 11 | yes | Type129PvpDodgeIncreased | Increases dodge in PvP by %s%%. |
| 129 | 12 | yes | Type129PvpDodgeReduced | Reduces dodge in PvP by %s%%. |
| 129 | 21 | yes | Type129NezarunRaidDamageIncreased | Increases damage to all monsters in the Nezarun and Crusher Nezarun raids by %s%%. |
| 129 | 22 | yes | Type129NezarunRaidDamageIncreasedNegated | Increases damage to all monsters in the Nezarun and Crusher Nezarun raids by %s%%. |
| 129 | 31 | yes | Type129NoExperience | Cannot earn experience of any kind. |
| 129 | 32 | yes | Type129NoExperienceNegated | Cannot earn experience of any kind. |
| 129 | 41 | yes | Type129TriggerOnAllyChance | Provides a %s%% chance to trigger %s on an ally. |
| 129 | 42 | yes | Type129TriggerOnAllyChanceNegated | Provides a %s%% chance to trigger %s on an ally. |
| 129 | 51 | no | Type129AttackPerStack |  |
| 129 | 52 | no | Type129DefencePerStack |  |
| 130 | 11 | no | HeroSummonAndSyncHeroSummonChanceOnAttackSkill |  |
| 130 | 12 | no | HeroSummonAndSyncHeroSummonChanceOnAttackSkillNegated |  |
| 130 | 21 | no | HeroSummonAndSyncHeroSummonChanceIncreased |  |
| 130 | 22 | no | HeroSummonAndSyncHeroSummonChanceIncreasedNegated |  |
| 130 | 31 | no | HeroSummonAndSyncSummonedHeroAttackIncreased |  |
| 130 | 32 | no | HeroSummonAndSyncSummonedHeroAttackIncreasedNegated |  |
| 130 | 41 | no | HeroSummonAndSyncFinalDamagePerSyncBuffLevel |  |
| 130 | 42 | no | HeroSummonAndSyncFinalDamagePerSyncBuffLevelNegated |  |
| 130 | 51 | no | HeroSummonAndSyncAttackWithDimensionalSynchronisation |  |
| 130 | 52 | no | HeroSummonAndSyncDefenceWithDimensionalSynchronisation |  |
| 131 | 11 | no | Type131RaidAttackIncreased |  |
| 131 | 12 | no | Type131RaidAttackIncreasedNegated |  |
| 131 | 21 | no | Type131DurationReducedOnBlackout |  |
| 131 | 22 | no | Type131DurationReducedOnBlackoutNegated |  |
| 131 | 31 | no | Type131StoredDamageTakenAppliedOnExpiry |  |
| 131 | 32 | no | Type131StoredDamageDealtAppliedOnExpiry |  |
| 131 | 41 | no | Type131WieldShadowSword |  |
| 131 | 42 | no | Type131WieldLightSword |  |
