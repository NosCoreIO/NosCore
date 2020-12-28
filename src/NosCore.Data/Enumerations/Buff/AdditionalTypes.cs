//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Buff
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class AdditionalTypes : BCardType
    {
        // 81-??
        public enum AbsorbedSpirit : byte
        {
            ApplyEffectIfPresent = 11,
            ApplyEffectIfNotPresent = 12,
            ResistForcedMovement = 21,
            ResistForcedMovementNegated = 22,
            MagicCooldownIncreased = 31,
            MagicCooldownDecreased = 32
        }

        public enum Absorption : byte
        {
            AllAttackIncreased = 11,
            AllAttackDecreased = 12,
            MeleeAttackIncreased = 21,
            MeleeAttacklDecreased = 22,
            RangedAttackIncreased = 31,
            RangedAttackDecreased = 32,
            MagicalAttackIncreased = 41,
            MagicalAttacksDecreased = 42
        }

        public enum AbsorptionAndPowerSkill : byte
        {
            AddDamageToHp = 11,
            RemoveDamnageFromHp = 12,

            // Unknown = 21, Unknown2 = 22, Unknown3 = 31, Unknown4 = 32,
            DamageIncreasedSkill = 41,

            DamageDecreasedSkill = 42,
            CriticalIncreasedSkill = 51,
            CriticalDecreasedSkill = 52
        }

        public enum AngerSkill : byte
        {
            AttackInRangeNotLocation = 11,
            AttackInRangeNotLocationNegated = 12,
            ReduceEnemyHpChance = 21,
            ReduceEnemyHpByDamageChance = 22,
            BlockGoodEffect = 31,
            BlockGoodEffectNegated = 32,
            OnlyNormalAttacks = 41,
            OnlyNormalAttacksNegated = 42
        }

        public enum ArenaCamera : byte
        {
            CallParticipant1 = 11,

            CallParticipant2 =
                12, // propably we will need to fix "Their" Mistake soo CallParticipant1 = 11 and 12, CallParticipant2 = 21 and 22 :D

            CallParticipant2Negated = 21,
            CallParticipant2NegatedNegated = 22,
            CallParticipant3 = 31,
            CallParticipant3Negated = 32,
            SwitchView = 41,
            SwitchViewNegated = 42,
            SeeHiddenAllies = 51,
            SeeHiddenAlliesNegated = 52
        }

        public enum AttackPower : byte
        {
            AllAttacksIncreased = 11,
            AllAttacksDecreased = 12,
            MeleeAttacksIncreased = 21,
            MeleeAttacksDecreased = 22,
            RangedAttacksIncreased = 31,
            RangedAttacksDecreased = 32,
            MagicalAttacksIncreased = 41,
            MagicalAttacksDecreased = 42,
            AttackLevelIncreased = 51,
            AttackLevelDecreased = 52
        }

        public enum BearSpirit : byte
        {
            IncreaseMaximumMp = 11,
            DecreaseMaximumMp = 12,

            // Unknown = 21, Unknown2 = 22,
            IncreaseMaximumHp = 31,

            DecreaseMaximumHp = 32
        }

        public enum Block : byte
        {
            ChanceAllIncreased = 11,
            ChanceAllDecreased = 12,
            ChanceMeleeIncreased = 21,
            ChanceMeleeDecreased = 22,
            ChanceRangedIncreased = 31,
            ChanceRangedDecreased = 32,
            ChanceMagicalIncreased = 41,
            ChanceMagicalDecreased = 42
        }

        public enum BossMonstersSkill : byte
        {
            InflictDamageAfter = 11,
            InflictDamageAfterNegated = 12
        }

        public enum Buff : byte
        {
            ChanceCausing = 11,
            ChanceRemoving = 12,
            PreventingBadEffect = 21,
            PreventingBadEffectNegated = 22,
            NearbyObjectsAboveLevel = 31,
            NearbyObjectsBelowLevel = 32,
            EffectResistance = 41,
            EffectResistanceNegated = 42,
            CancelGroupOfEffects = 51,
            CounteractPoison = 52
        }

        public enum CalculatingLevel : byte
        {
            CalculatedAttackLevel = 11,
            CalculatedAttackLevelNegated = 12,
            CalculatedDefenceLevel = 21,
            CalculatedDefenceLevelNegated = 22
        }

        public enum Capture : byte
        {
            CaptureAnimal = 11,
            CaptureAnimalNegated = 12
        }

        public enum Casting : byte
        {
            EffectDurationIncreased = 11,
            EffectDurationDecreased = 12,
            ManaForSkillsIncreased = 21,
            ManaForSkillsDecreased = 22,
            AttackSpeedIncreased = 31,
            AttackSpeedDecreased = 32,
            CastingSkillFailed = 41,
            CastingSkillFailedNegated = 42,
            InterruptCasting = 51,
            InterruptCastingNegated = 52
        }

        public enum Count : byte
        {
            Summon = 11,
            SummonChance = 12
        }

        public enum Critical : byte
        {
            InflictingIncreased = 11,
            InflictingReduced = 12,
            DamageIncreased = 21,
            DamageIncreasedInflictingReduced = 22,
            DamageIncreasingPropability = 31,
            DamageReducingPropability = 32,
            ReceivingIncreased = 41,
            ReceivingDecreased = 42,
            DamageFromCriticalIncreased = 51,
            DamageFromCriticalDecreased = 52
        }

        public enum Damage : byte
        {
            DamageIncreased = 11,
            DamageDecreased = 12,
            MeleeIncreased = 21,
            MeleeDecreased = 22,
            RangedIncreased = 31,
            RangedDecreased = 32,
            MagicalIncreased = 41,
            MagicalDecreased = 42
        }

        public enum DamageConvertingSkill : byte
        {
            TransferInflictedDamage = 11,
            TransferInflictedDamageNegated = 12,
            IncreaseDamageTransfered = 21,
            DecreaseDamageTransfered = 22,
            HpRecoveryIncreased = 31,
            HpRecoveryDecreased = 32,
            AdditionalDamageCombo = 41,
            AdditionalDamageComboNegated = 42,
            ReflectMaximumReceivedDamage = 51,
            ReflectMaximumReceivedDamageNegated = 52
        }

        public enum DarkCloneSummon : byte
        {
            SummonDarkCloneChance = 11,
            SummonDarkCloneChanceNegated = 12,
            ConvertRecoveryToDamage = 21,
            ConvertRecoveryToDamageNegated = 22,
            ConvertDamageToHpChance = 31,
            ConvertDamageToHpChanceNegated = 32,
            IncreaseEnemyCooldownChance = 41,
            IncreaseEnemyCooldownChanceNegated = 42,
            DarkElementDamageIncreaseChance = 51,
            DarkElementDamageDecreaseChance = 52
        }

        public enum DebuffResistance : byte
        {
            IncreaseBadEffectChance = 11,
            NeverBadEffectChance = 12,
            IncreaseBadGeneralEffectChance = 21,
            NeverBadGeneralEffectChance = 22,
            IncreaseBadMagicEffectChance = 31,
            NeverBadMagicEffectChance = 32,
            IncreaseBadToxicEffectChance = 41,
            NeverBadToxicEffectChance = 42,
            IncreaseBadDiseaseEffectChance = 51,
            NeverBadDiseaseEffectChance = 52
        }

        public enum Defence : byte
        {
            AllIncreased = 11,
            AllDecreased = 12,
            MeleeIncreased = 21,
            MeleeDecreased = 22,
            RangedIncreased = 31,
            RangedDecreased = 32,
            MagicalIncreased = 41,
            MagicalDecreased = 42,
            DefenceLevelIncreased = 51,
            DefenceLevelDecreased = 52
        }

        public enum DodgeAndDefencePercent : byte
        {
            DodgeIncreased = 11,
            DodgeDecreased = 12,
            DodgingMeleeIncreased = 21,
            DodgingMeleeDecreased = 22,
            DodgingRangedIncreased = 31,
            DodgingRangedDecreased = 32,
            DefenceIncreased = 41,
            DefenceReduced = 42
        }

        public enum Drain : byte
        {
            CastDrain = 11,
            CastDrainNegated = 12,
            TransferEnemyHp = 21,
            TransferEnemyHpNegated = 22
        }

        // 21-40
        public enum DrainAndSteal : byte
        {
            ReceiveHpFromMp = 11,
            ReceiveHpFromMpNegated = 12,
            ReceiveMpFromHp = 21,
            ReceiveMpFromHpNegated = 22,
            GiveEnemyHp = 31,
            LeechEnemyHp = 32,
            GiveEnemyMp = 41,
            LeechEnemyMp = 42,
            ConvertEnemyMptoHp = 51,
            ConvertEnemyHptoMp = 52
        }

        public enum Element : byte
        {
            FireIncreased = 11,
            FireDecreased = 12,
            WaterIncreased = 21,
            WaterDecreased = 22,
            LightIncreased = 31,
            LightDecreased = 32,
            DarkIncreased = 41,
            DarkDecreased = 42,
            AllIncreased = 51,
            AllDecreased = 52
        }

        public enum ElementResistance : byte
        {
            AllIncreased = 11,
            AllDecreased = 12,
            FireIncreased = 21,
            FireDecreased = 22,
            WaterIncreased = 31,
            WaterDecreased = 32,
            LightIncreased = 41,
            LightDecreased = 42,
            DarkIncreased = 51,
            DarkDecreased = 52
        }

        public enum EnemyElementResistance : byte
        {
            AllIncreased = 11,
            AllDecreased = 12,
            FireIncreased = 21,
            FireDecreased = 22,
            WaterIncreased = 31,
            WaterDecreased = 32,
            LightIncreased = 41,
            LightDecreased = 42,
            DarkIncreased = 51,
            DarkDecreased = 52
        }

        public enum FairyXpIncrease : byte
        {
            TeleportToLocation = 11,
            TeleportToLocationNegated = 12,
            IncreaseFairyXpPoints = 21,
            IncreaseFairyXpPointsNegated = 22
        }

        public enum FalconSkill : byte
        {
            CausingChanceLocation = 11,
            RemovingChanceLocation = 12,
            Hide = 21,
            HideNegated = 22,
            Ambush = 31,
            AmbushNegated = 32,
            FalconFollowing = 41,
            FalconFollowingNegated = 42,
            FalconFocusLowestHp = 51,
            FalconFocusLowestHpNegated = 52
        }

        public enum FearSkill : byte
        {
            RestoreRemainingEnemyHp = 11,
            DecreaseRemainingEnemyHp = 12,
            TimesUsed = 21,
            TimesUsedNegated = 22,
            AttackRangedIncreased = 31,
            AttackRangedDecreased = 32,
            MoveAgainstWill = 41,
            MoveAgainstWillNegated = 42,
            ProduceWhenAmbushe = 51,
            ProduceWhenAmbushNegated = 52
        }

        public enum FireCannoneerRangeBuff : byte
        {
            AoeIncreased = 11,
            AoeDecreased = 12,
            Flinch = 21,
            FlinchNegated = 22
        }

        public enum FocusEnemyAttentionSkill : byte
        {
            FocusEnemyAttention = 11

            // Unknown = 12, Unknown2 = 21, Unknown3 = 22,
        }

        public enum FourthGlacernonFamilyRaid : byte
        {
            AllInFieldReceiveDamage = 11, // Look nearly the same as 12
            AllInFieldsReceiveDamage = 12 // Look nearly the same as 11
        }

        public enum FrozenDebuff : byte
        {
            MovementLocked = 11,
            MovementLockedNegated = 12

            // Unknown = 21, Unknown2 = 22
        }

        public enum GuarantedDodgeRangedAttack : byte
        {
            AttackHitChance = 11,
            AttackHitChanceNegated = 12,
            AlwaysDodgePropability = 21,
            AlwaysDodgePropabilityNegated = 22,
            NoPenatly = 41,
            DistanceDamageIncreasing = 41,
            NoPenatlyNegated = 42,
            DistanceDamageIncreasingNegated = 42
        }

        public enum HealingBurningAndCasting : byte
        {
            RestoreHp = 11,
            DecreaseHp = 12,
            RestoreMp = 21,
            DecreaseMp = 22,
            RestoreHpWhenCasting = 31,
            DecreaseHpWhenCasting = 32,
            RestoreHpWhenCastingInterrupted = 41,
            DecreaseHpWhenCastingInterrupted = 42,
            HpIncreasedByConsumingMp = 51,
            HpDecreasedByConsumingMp = 52
        }

        // 61-80
        /*public enum Missingno : byte
        {
            Unknown = 11,
            Unknown2 = 12
        }*/

        public enum HideBarrelSkill : byte
        {
            NoHpConsumption = 11,
            NoHpRecovery = 12
        }

        public enum Hpmp : byte
        {
            RestoreDecreasedHp = 11,
            DecreaseRemainingHp = 12,
            RestoreDecreasedMp = 21,
            DecreaseRemainingMp = 22,
            HpRestored = 31,
            HpReduced = 32,
            MpRestored = 41,
            MpReduced = 42,
            ReceiveAdditionalHp = 51,
            ReceiveAdditionalMp = 52
        }

        public enum HugeSnowman : byte
        {
            SnowStorm = 11,
            SnowStormNegated = 12,
            EarthQuake = 21,
            EarthQuakeNegated = 22
        }

        public enum IncreaseDamage : byte
        {
            IncreasingPropability = 11,
            DecreasingPropability = 12,
            FireIncreased = 21,
            FireDecreased = 22,
            WaterIncreased = 31,
            WaterDecreased = 32,
            LightIncreased = 41,
            LightDecreased = 42,
            DarkIncreased = 51,
            DarkDecreased = 52
        }

        public enum InflictSkill : byte
        {
            InflictDamageAtLocation = 11,
            InflictDamageAtLocationNegated = 12
        }

        public enum Item : byte
        {
            ExpIncreased = 11,
            ExpIncreasedNegated = 12,
            AttackIncreased = 21,
            DefenceIncreased = 22,
            DropItemsWhenAttacked = 31,
            DropItemsWhenAttackedNegated = 32,
            ScrollPower = 41,
            ScrollPowerNegated = 42,
            IncreaseEarnedGold = 51,
            IncreaseEarnedGoldNegated = 52
        }

        public enum JumpBackPush : byte
        {
            JumpBackChance = 11,

            // Unknown = 12,
            PushBackChance = 21,

            PushBackChanceNegated = 22,
            MeleeDurationIncreased = 31,
            MeleeDurationDecreased = 32,
            RangedDurationIncreased = 41,
            RangedDurationDecreased = 42,
            MagicalDurationIncreased = 51,
            MagicalDurationDecreased = 52
        }

        public enum LeonaPassiveSkill : byte
        {
            IncreaseDamageAgainst = 11,
            DecreaseDamageAgainst = 12,
            IncreaseRecoveryItems = 21,
            DecreaseRecoveryItems = 22,
            OnSpWearCausing = 31,
            OnSpWearRemoving = 32,
            DefenceIncreasedInPvp = 41,
            DefenceDecreasedInPvp = 42,
            AttackIncreasedInPvp = 51,
            AttackDecreasedInPvp = 52
        }

        public enum LightAndShadow : byte
        {
            InflictDamageToMp = 11,
            IncreaseMpByAbsorbedDamage = 12,
            RemoveBadEffects = 21,
            RemoveGoodEffects = 22,
            InflictDamageOnUndead = 31,
            HealUndead = 32,
            AdditionalDamageWhenHidden = 41,
            AdditionalDamageOnHiddenEnemy = 42
        }

        public enum LordCalvinas : byte
        {
            InflictDamageAtLocation = 11,
            InflictDamageAtLocationNegated = 12
        }

        public enum LordHatus : byte
        {
            InflictDamageAtLocation = 11,
            InflictDamageAtLocationNegated = 12
        }

        public enum MaxHpmp : byte
        {
            MaximumHpIncreased = 11,
            MaximumHpDecreased = 12,
            MaximumMpIncreased = 21,
            MaximumMpDecreased = 22,
            IncreasesMaximumHp = 31,
            DecreasesMaximumHp = 32,
            IncreasesMaximumMp = 41,
            DecreasesMaximumMp = 42,
            MaximumHpmpIncreased = 51,
            MaximumHpmpDecreased = 52
        }

        public enum MeditationSkill : byte
        {
            CausingChance = 11,
            RemovingChance = 12,
            ShortMeditation = 21,
            ShortMeditationNegated = 22,
            RegularMeditation = 31,
            RegularMeditationNegated = 32,
            LongMeditation = 41,
            LongMeditationNegated = 42,
            Sacrifice = 51,
            Sacrificenegated = 52
        }

        public enum MeteoriteTeleport : byte
        {
            SummonInVisualRange = 11,
            SummonInVisualRangeNegated = 12,
            TransformTarget = 21,
            TransformTargetNegated = 22,
            TeleportForward = 31,
            TeleportForwardNegated = 32,
            CauseMeteoriteFall = 41,
            CauseMeteoriteFallNegated = 42,
            TeleportYouAndGroupToSavedLocation = 51,
            TeleportYouAndGroupToSavedLocationNegated = 52
        }

        // 41-60
        public enum Mode : byte
        {
            Range = 11,
            ReturnRange = 12,
            EffectNoDamage = 21,
            DirectDamage = 22,
            AttackTimeIncreased = 31,
            AttackTimeDecreased = 32,
            ModeChance = 41,
            ModeChanceNegated = 42,
            OccuringChance = 51,
            OccuringChanceNegated = 52
        }

        public enum Morale : byte
        {
            MoraleIncreased = 11,
            MoraleDecreased = 12,
            MoraleDoubled = 21,
            MoraleHalved = 22,
            LockMorale = 31,
            LockMoraleNegated = 32,
            SkillCooldownIncreased = 41,
            SkillCooldownDecreased = 42,
            IgnoreEnemyMorale = 51,
            IgnoreEnemyMoraleNegated = 52
        }

        public enum Move : byte
        {
            MovementImpossible = 11,
            MovementImpossibleNegated = 12,
            MoveSpeedIncreased = 21,
            MoveSpeedDecreased = 22,
            SetMovement = 31,
            SetMovementNegated = 32,
            MovementSpeedIncreased = 41,
            MovementSpeedDecreased = 42,
            TempMaximized = 51,
            TempMaximizedNegated = 52
        }

        public enum MultAttack : byte
        {
            AllAttackIncreased = 11,
            AllAttackDecreased = 12,
            MeleeAttackIncreased = 21,
            MeleeAttackDecreased = 22,
            RangedAttackIncreased = 31,
            RangedAttackDecreased = 32,
            MagicalAttackIncreased = 41,
            MagicalAttackDecreased = 42
        }

        public enum MultDefence : byte
        {
            AllDefenceIncreased = 11,
            AllDefenceDecreased = 12,
            MeleeDefenceIncreased = 21,
            MeleeDefenceDecreased = 22,
            RangedDefenceIncreased = 31,
            RangedDefenceDecreased = 32,
            MagicalDefenceIncreased = 41,
            MagicalDefenceDecreased = 42
        }

        public enum NoCharacteristicValue : byte
        {
            AllPowersNullified = 11,
            AllResistancesNullified = 12,
            FireElementNullified = 21,
            FireResistanceNullified = 22,
            WaterElementNullified = 31,
            WaterResistanceNullified = 32,
            LightElementNullified = 41,
            LightResistanceNullified = 42,
            DarkElementNullified = 51,
            DarkResistanceNullified = 52
        }

        public enum NoDefeatAndNoDamage : byte
        {
            DecreaseHpNoDeath = 11,
            DecreaseHpNoKill = 12,
            NeverReceiveDamage = 21,
            NeverCauseDamage = 22,
            TransferAttackPower = 31,
            TransferAttackPowerNegated = 32
        }

        public enum Quest : byte
        {
            SummonMonsterBased = 11,
            SummonMonsterBasedNegated = 12
        }

        public enum Recovery : byte
        {
            HpRecoveryIncreased = 11,
            HpRecoveryDecreased = 12,
            MpRecoveryIncreased = 21,
            MpRecoveryDecreased = 22
        }

        public enum RecoveryAndDamagePercent : byte
        {
            HpRecovered = 11,
            HpReduced = 12,
            MpRecovered = 21,
            MpReduced = 22,
            DecreaseEnemyHp = 31,
            DecreaseSelfHp = 32
        }

        public enum Reflection : byte
        {
            HpIncreased = 11,
            HpDecreased = 12,
            MpIncreased = 21,
            MpDecreased = 22,
            EnemyHpIncreased = 31,
            EnemyHpDecreased = 32,
            EnemyMpIncreased = 41,
            EnemyMpDecreased = 42
        }

        public enum SecondSpCard : byte
        {
            PlantBomb = 11,
            SetBombWhenAttack = 12, // Same as 22!
            PlantSelfDestructionBomb = 21,
            PlantBombWhenAttack = 22, // Same as 12!
            ReduceEnemySkill = 31,
            ReduceEnemySkillNegated = 32,
            HitAttacker = 41,
            HitAttackerNegated = 42
        }

        public enum SeSpecialist : byte
        {
            EnterNumberOfBuffsAndDamage = 12,
            EnterNumberOfBuffs = 22,
            MovingAura = 31,
            DontNeedToEnter = 32, // Same as 42
            LowerHpStrongerEffect = 41,
            DoNotNeedToEnter = 42 // Same as 32
        }

        public enum SniperAttack : byte
        {
            ChanceCausing = 11,
            ChanceRemoving = 12,
            AmbushRangeIncreased = 21,
            AmbushRangeIncreasedNegated = 22,
            ProduceChance = 31,
            ProduceChanceNegated = 32,
            KillerHpReducing = 41,
            KillerHpIncreasing = 42,
            ReceiveCriticalFromSniper = 51,
            ReceiveCriticalFromSniperNegated = 52
        }

        public enum SpCardUpgrade : byte
        {
            LowerSpScroll = 11,
            LowerSpScrollNegated = 12,
            HigherSpScroll = 21,
            HigherSpScrollNegated = 22
        }

        public enum SpecialActions : byte
        {
            PushBack = 11,
            PushBackNegated = 12,
            FocusEnemies = 21,
            FocusEnemiesNegated = 22,
            Charge = 31,
            ChargeNegated = 32,
            RunAway = 41,
            RunAwayNegated = 42,
            Hide = 51,
            SeeHiddenThings = 52
        }

        // 1-20
        public enum SpecialAttack : byte
        {
            NoAttack = 11,
            NoAttackNegated = 12,
            MeleeDisabled = 21,
            MeleeDisabledNegated = 22,
            RangedDisabled = 31,
            RangedDisabledNegated = 32,
            MagicDisabled = 41,
            MagicDisabledNegated = 42,
            FailIfMiss = 51,
            FailIfMissNegated = 52
        }

        public enum SpecialBehaviour : byte
        {
            TeleportRandom = 11,
            TeleportRandomNegated = 12,
            JumpToEveryObject = 21,
            JumpToEveryObjectNegated = 22,
            InflictOnTeam = 31,
            InflictOnEnemies = 32,
            TransformInto = 41,
            TransformIntoNegated = 42
        }

        public enum SpecialCritical : byte
        {
            AlwaysInflict = 11,
            AlwaysInflictNegated = 12,
            NeverInflict = 21,
            NeverInflictNegated = 22,
            AlwaysReceives = 31,
            AlwaysReceivesNegated = 32,
            NeverReceives = 41,
            NeverReceivesNegated = 42,
            InflictingChancePercent = 51,
            ReceivingChancePercent = 52
        }

        public enum SpecialDamageAndExplosions : byte
        {
            ChanceExplosion = 11,
            ChanceExplosionNegated = 12,
            ExplosionCauses = 21,
            ExplosionCausesNegated = 22,
            SurroundingDamage = 31,
            SurroundingDamageNegated = 32
        }

        public enum SpecialDefence : byte
        {
            AllDefenceNullified = 11,
            AllDefenceNullifiedNegated = 12,
            MeleeDefenceNullified = 21,
            MeleeDefenceNullifiedNegated = 22,
            RangedDefenceNullified = 31,
            RangedDefenceNullifiedNegated = 32,
            MagicDefenceNullified = 41,
            MagicDefenceNullifiedNegated = 42,
            NoDefence = 51,
            NoDefenceNegated = 52
        }

        public enum SpecialEffects : byte
        {
            DecreaseKillerHp = 11,
            IncreaseKillerHp = 12,
            ToPrefferedAttack = 21,
            ToNonPrefferedAttack = 22,
            Gibberish = 31,
            GibberishNegated = 32,
            AbleToFightPvp = 41,
            AbleToFightPvpNegated = 42,
            ShadowAppears = 51,
            ShadowAppearsNegated = 52
        }

        public enum SpecialEffects2 : byte
        {
            FocusEnemy = 11,
            RemoveEnemyAttention = 12,
            TeleportInRadius = 21,
            TeleportInRadiusNegated = 22,
            MainWeaponCausingChance = 31,
            MainWeaponCausingChanceNegated = 32,
            SecondaryWeaponCausingChance = 41,
            SecondaryWeaponCausingChanceNegated = 42,
            BefriendMonsters = 51,
            BefriendMonstersNegated = 52
        }

        public enum SpecialisationBuffResistance : byte
        {
            IncreaseDamageAgainst = 11,
            ReduceDamageAgainst = 12,
            IncreaseCriticalAgainst = 21,
            ReduceCriticalAgainst = 22,
            ResistanceToEffect = 31,
            ResistanceToEffectNegated = 32,
            IncreaseDamageInPvp = 41,
            DecreaseDamageInPvp = 42,
            RemoveBadEffects = 52
        }

        public enum Spsl : byte
        {
            Attack,
            Defense,
            Element,
            Hpmp,
            All
        }

        public enum StealBuff : byte
        {
            IgnoreDefenceChance = 11,
            IgnoreDefenceChanceNegated = 12,
            ReduceCriticalReceivedChance = 21,
            ReduceCriticalReceivedChanceNegated = 22,
            ChanceSummonOnyxDragon = 31,
            ChanceSummonOnyxDragonNegated = 32,
            StealGoodEffect = 41,
            StealGoodEffectNegated = 42
        }

        public enum SummonAndRecoverHp : byte
        {
            ChanceSummon = 11,
            ChanceSummonNegated = 12,
            RestoreHp = 21,
            ReduceHp = 22
        }

        public enum SummonedMonsterAttack : byte
        {
            CauseDamage = 11,
            CauseDamageNegated = 12
        }

        public enum Summons : byte
        {
            SummonUponDeath = 11,
            SummonUponDeathChance = 12,
            Summons = 21,
            SummonningChance = 22,
            SummonTrainingDummy = 31,
            SummonTrainingDummyChance = 32,
            SummonTimedMonsters = 41,
            SummonTimedMonstersChance = 42,
            SummonGhostMp = 51,
            SummonGhostMpChance = 52
        }

        public enum SummonSkill : byte
        {
            // Unknown = 11, Unknown2 = 12, Unknown3 = 21, Unknown4 = 22,
            Summon = 31,

            SummonTimed = 32
        }

        public enum Target : byte
        {
            AllHitRateIncreased = 11,
            AllHitRateDecreased = 12,
            MeleeHitRateIncreased = 21,
            MeleeHitRateDecreased = 22,
            RangedHitRateIncreased = 31,
            RangedHitRateDecreased = 32,
            MagicalConcentrationIncreased = 41,
            MagicalConcentrationDecreased = 42
        }

        public enum TauntSkill : byte
        {
            ReflectsMaximumDamageFrom = 11,
            ReflectsMaximumDamageFromNegated = 12,
            DamageInflictedIncreased = 21,
            DamageInflictedDecreased = 22,
            EffectOnKill = 31,
            EffectOnKillNegated = 32,
            TauntWhenKnockdown = 41,
            TauntWhenNormal = 42,
            ReflectBadEffect = 51,
            ReflectBadEffectNegated = 52
        }

        public enum TeamArenaBuff : byte
        {
            DamageTakenIncreased = 11,
            DamageTakenDecreased = 12,
            AttackPowerIncreased = 21,
            AttackPowerDecreased = 22
        }

        public enum TimeCircleSkills : byte
        {
            GatherEnergy = 11,
            GatherEnergyNegated = 12,
            DisableHpConsumption = 21,
            DisableHpRecovery = 22,
            DisableMpConsumption = 31,
            DisableMpRecovery = 32,
            CancelAllBuff = 41,
            CancelAllBuffNegated = 42,
            ItemCannotBeUsed = 51,
            ItemCannotBeUsedNegated = 52
        }

        public enum VulcanoElementBuff : byte
        {
            SkillsIncreased = 11,
            SkillsDecreased = 12,
            ReducesEnemyAttack = 21,
            ReducesEnemyAttackNegated = 22,
            PullBackBuffIncreasing = 31,
            PullBackBuffIncreasingNegated = 32,
            CriticalDefence = 41,
            CriticalDefenceNegated = 42
        }

        public enum Type85 : byte
        {
            PositionSwapper = 11,
            OnDeathIncreaseRepLost = 21,
            OnDeathReduceRepLost = 22,
            OnDeathIncreaseHxpLost = 31,
            OnDeathReduceHxpLost = 32,
            IncreaseDamageVsAngels = 41,
            ReduceDamageVsAngels = 42,
            IncreaseDamageVsDeamons = 51,
            ReduceDamageVsDeamons = 52
        }

        public enum Type86 : byte
        {
            CdResetWithProb = 11,
            IncreaseGrovyBeachVibesProb = 21,
            DecreaseGrovyBeachVibesProb = 22,
            IncreaseDamageVsHigherLevelMonster = 31,
            DecreaseDamageVsHigherLevelMonster = 32,
            BlockAmountOfBadEffect = 41,
            ContagiousDebuffAroundCell = 51
        }

        public enum Type87 : byte
        {
            IncreaseFameRecived = 11,
            DecreaseFameRecived = 12,
            IncreaseHxpRecived = 21,
            DecreaseHxpRecived = 22,
            ReciveRandomItem = 31,
            RemoveBuffFromEnemy = 41,
            Unknow = 51
        }

        public enum Type88 : byte
        {
            MateReturnToMiniland = 11,
            IncreaseDamageVsHiden = 21,
            DecreaseDamageVsHiden = 22,
            IncreaseDamageFromHiden = 31,
            DecreaseDamageFromHiden = 32,
            RevealsHidenEnemy = 41,
            SkillResetXTimes = 51
        }

        public enum Type89 : byte
        {
            Transform = 11,
            RemoveTransform = 12,
            Unknow = 21,
            NegateGoodEffect = 31,
            NormalAndJobExpIncreased = 41,
            ZhephyrMagicalArrow = 51
        }

        public enum Type90 : byte
        {
            DamageVsSealedAndReduceFireDamage = 00, // Yeah... SubType 0 and double Effect o_O
            //SubType 1 didn't exist
            IncreaseDamageVsSealed = 21,
            DecreaseDamageVsSealed = 22,
            RainbowBattleDamageAndSpeedBoost = 31, // Another double Effect
            IncreaseDamageVsSealedAndGlacernonMonsters = 41, // Another double Effect
            DecreaseDamageVsSealedAndGlacernonMonsters = 42
        }

        public enum Type91 : byte
        {
            AddBuff = 11,
            BuffSkillBooster = 21,
            AllowFullMoonSkill = 31,
            AllowLotusPowerSkill = 41,
            ReduceCriticalDamageRecived = 51,
            IncreaseCriticalDamageRecived = 52
        }

        public enum Type92 : byte
        {
            AddBuffOnReceiveAttack = 11,
            ChanceToReciveOpportunityToAttack = 21,
            //SubType 3 didn't exist
            TransformEnemyDebuff = 41
        }

        public enum Type93 : byte
        {
            IncreaseElementOnAttack = 00,
            DecreaseElementOnAttack = 01,
            IncreaseFireElement = 11,
            DecreaseFireElement = 12,
            IncreaseWaterElement = 21,
            DecreaseWaterElement = 22,
            //SubType 3 didn't exist. IncreaseLightElement ?
            //SubType 4 didn't exist. IncreaseDarkElement ?
            IncreaseAllElements = 51,
            DecreaseAllElements = 52
        }

        public enum Type94 : byte
        {
            IncreaseAllElementsRes = 11,
            DecreaseAllElementsRes = 12,
            IncreaseFireRes = 21,
            DecreaseFireRes = 22,
            IncreaseWaterRes = 31,
            DecreaseWaterRes = 32,
            //SubType 4 didn't exist. IncreaseLightRes ?
            IncreaseDarkRes = 51,
            DecreaseDarkRes = 52
        }

        public enum Type95 : byte
        {
            DropX2Chance = 11,
            AddBuffOnAttack = 21,
            //SubType 3 didn't exist.
            AddBuffOnDef = 41,
        }

        public enum Type96 : byte
        {
            IncreaseFairyElement = 11,
            DecreaseFairyElement = 12,
            IncreaseFairyElementOnAttack = 21,
            DecreaseFairyElementOnAttack = 22,
            IncreaseDamageToMonsters = 31,
            DecreaseDamageToMonsters = 32
        }

        public enum Type97 : byte
        {
            AddBuffWithMissingHpChance = 11,
            IncreaseUltimatePoins = 21,
            DecreaseUltimatePoints = 22,
            AllowUltimateSkills = 31,
            DarkElementAddBuff = 41,
            IncreaseAttackAndDefenceForDebuff = 51,
            DecreaseAttackAndDefenceForDebuff = 52
        }

        public enum Type98 : byte
        {
            IncreaseWaterDamage = 00,
            DecreaseWaterDamage = 01,
            IncreaseAllAttackDamageRecived = 11,
            DecreaseAllAttackDamageRecived = 12,
            IncreaseFireAttackDamage = 21,
            DecreaseFireAttackDamage = 22
        }

        public enum Type99 : byte
        {
            IncreaseDamageToLowLevelPlant = 11,
            DecreaseDamageToLowLevelPlant = 12,
            IncreaseDamageToLowLevelAnimal = 21,
            DecreaseDamageToLowLevelAnimal = 22,
            IncreaseDamageToLowLevelMonster = 31,
            DecreaseDamageToLowLevelMonster = 32,
            IncreaseDamageToKovolt = 41,
            DecreaseDamageToKovolt = 42,
            IncreaseDamageToCatsie = 51,
            DecreaseDamageToCatsie = 52
        }

        public enum Type100 : byte
        {
            IncreaseDamageToLowLevelSpirit = 11,
            DecreaseDamageToLowLevelSpirit = 12,
            IncreaseDamageToAngel = 21,
            DecreaseDamageToAngel = 22,
            IncreaseDamageToDemon = 31,
            DecreaseDamageToDemon = 32,
            IncreaseDamageToLowLevelUndead = 41,
            DecreaseDamageToLowLevelUndead = 42,
            IncreaseProductionPointConsume = 51,
            DecreaseProductionPointConsume = 52
        }

        public enum Type101 : byte
        {
            IncreaseDamageToLODMonster = 11,
            DecreaseDamageToLODMonster = 12,
            IncreaseDamageToSealed = 21,
            DecreaseDamageToSealed = 22,
            IncreaseXpGain = 31,
            DecreaseXpGain = 32,
            IncreaseJXpGain = 41,
            DecreaseJXpGain = 42,
            IncreaseDodge = 51,
            DecreaseDodge = 52
        }

        public enum Type102 : byte
        {
            IncreaseSLDamage = 11,
            DecreaseSLDamage = 12,
            IncreaseSLDefence = 21,
            DecreaseSLDefence = 22,
            IncreaseSLElement = 31,
            DecreaseSLElement = 32,
            IncreaseSLHp = 41,
            DecreaseSLHp = 42,
            IncreaseHitRate = 51,
            DecreaseHitRate = 52
        }

        public enum Type103 : byte
        {
            IncreaseAllAttack = 11,
            DecreaseAllAttack = 12,
            IncreaseMeleeAttack = 21,
            DecreaseMeleeAttack = 22,
            IncreaseRangedAttack = 31,
            DecreaseRangedAttack = 32,
            IncreaseMagicAttack = 41,
            DecreaseMagicAttack = 42,
            IncreaseConcentration = 51,
            DecreaseConcentration = 52
        }

        public enum Type104 : byte
        {
            ReflectOnDeff = 11,
            AreaDamageEachSecond = 21,
            SummonMonsterOnDef = 31,
            //SubType 4 didn't exist.
            AreaBuffEachSecond = 51
        }

        public enum Type107 : byte
        {
            IncreaseMagicDamage = 11,
            DecreaseMagicDamage = 12,
            IncreaseElementResOnDef = 21,
            DecreaseElementResOnDef = 22,
            IncreaseXpGain = 31,
            DecreaseXpGain = 32,
            //SubType 4 didn't exist.
            IncreaseDodge = 51,
            DecreaseDodge = 52
        }

        public enum Type108 : byte
        {
            ReflectCritOnDef = 11,
            IncreaseDodgeByMissingHp = 21,
            DecreaseDodgeByMissingHp = 22,
            IncreaseCritDamageWithCount = 31,
            DecreaseCritDamageWithCount = 32,
            TransferDamageToMp = 41,
            IncreaseAttackByMagicRes = 51,
            DecreaseAttackByMagicRes = 52
        }

        public enum Type109 : byte
        {
            //SubType 1 didn't exist.
            //SubType 2 didn't exist.
            //SubType 3 didn't exist.
            ChanceToIncreaseDamage = 41,
            ChanceToDecreaseDamage = 42,
            IncreaseElementOnDebuff = 51,
            DecreaseElementOnDebuff = 52
        }

        public enum Type110 : byte
        {
            RecoveryHpOnDodge = 11,
            DecreaseHpOnDodge = 12,
            BeastKingTarget = 21
        }

        // end!
    }
}