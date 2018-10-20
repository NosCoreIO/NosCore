//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

namespace NosCore.Shared.Enumerations.Buff
{
    public class BCardType
    {
        #region Enums

        public enum CardType : byte
        {
            // 1-20
            SpecialAttack = 1,

            SpecialDefence = 2,
            AttackPower = 3,
            Target = 4,
            Critical = 5,
            SpecialCritical = 6,
            Element = 7,
            IncreaseDamage = 8,
            Defence = 9,
            DodgeAndDefencePercent = 10,
            Block = 11,
            Absorption = 12,
            ElementResistance = 13,
            EnemyElementResistance = 14,
            Damage = 15,
            GuarantedDodgeRangedAttack = 16,
            Morale = 17,
            Casting = 18,
            Move = 19,
            Reflection = 20,

            // 21-40
            DrainAndSteal = 21,

            HealingBurningAndCasting = 22,
            Hpmp = 23,
            SpecialisationBuffResistance = 24,
            Buff = 25,
            Summons = 26,
            SpecialEffects = 27,
            Capture = 28,
            SpecialDamageAndExplosions = 29,
            SpecialEffects2 = 30,
            CalculatingLevel = 31,
            Recovery = 32,
            MaxHpmp = 33,
            MultAttack = 34,
            MultDefence = 35,
            TimeCircleSkills = 36,
            RecoveryAndDamagePercent = 37,
            Count = 38,
            NoDefeatAndNoDamage = 39,
            SpecialActions = 40,

            // 41-60
            Mode = 41,

            NoCharacteristicValue = 42,
            LightAndShadow = 43,
            Item = 44,
            DebuffResistance = 45,
            SpecialBehaviour = 46,
            Quest = 47,
            SecondSpCard = 48,
            SpCardUpgrade = 49,
            HugeSnowman = 50,
            Drain = 51,
            BossMonstersSkill = 52,
            LordHatus = 53,
            LordCalvinas = 54,
            SeSpecialist = 55,
            SummonedMonsterAttack = 56,
            FourthGlacernonFamilyRaid = 57,
            BearSpirit = 58,
            SummonSkill = 59,
            InflictSkill = 60,

            // 61-80 Missingno = 61,
            HideBarrelSkill = 62,

            FocusEnemyAttentionSkill = 63,
            TauntSkill = 64,
            FireCannoneerRangeBuff = 65,
            VulcanoElementBuff = 66,
            DamageConvertingSkill = 67,
            MeditationSkill = 68,
            FalconSkill = 69,
            AbsorptionAndPowerSkill = 70,
            LeonaPassiveSkill = 71,
            FearSkill = 72,
            SniperAttack = 73,
            FrozenDebuff = 74,
            JumpBackPush = 75,
            FairyXpIncrease = 76,
            SummonAndRecoverHp = 77,
            TeamArenaBuff = 78,
            ArenaCamera = 79,
            DarkCloneSummon = 80,

            // 81-??
            AbsorbedSpirit = 81,

            AngerSkill = 82,
            MeteoriteTeleport = 83,
            StealBuff = 84,

            Spsl = 200
        }
    }

    #endregion
}