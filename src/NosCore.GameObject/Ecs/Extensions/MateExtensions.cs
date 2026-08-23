//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using System.Globalization;

namespace NosCore.GameObject.Ecs.Extensions
{
    public static class MateExtensions
    {
        public static ScpPacket GenerateScp(this Mate mate, RegionType language)
        {
            return new ScpPacket
            {
                PetId = mate.PetSlot,
                NpcMonsterVNum = mate.VNum,
                TransportId = mate.MateTransportId,
                Level = mate.Level,
                Loyalty = mate.Loyalty,
                Experience = mate.Experience,
                Unknow1 = 0,
                AttackUpgrade = mate.NpcMonster.AttackUpgrade,
                DamageMinimum = mate.NpcMonster.DamageMinimum,
                DamageMaximum = mate.NpcMonster.DamageMaximum,
                Concentrate = mate.NpcMonster.Concentrate,
                CriticalChance = mate.NpcMonster.CriticalChance,
                CriticalRate = mate.NpcMonster.CriticalRate,
                DefenceUpgrade = mate.NpcMonster.DefenceUpgrade,
                CloseDefence = mate.NpcMonster.CloseDefence,
                DefenceDodge = mate.NpcMonster.DefenceDodge,
                DistanceDefence = mate.NpcMonster.DistanceDefence,
                DistanceDefenceDodge = mate.NpcMonster.DistanceDefenceDodge,
                MagicDefence = mate.NpcMonster.MagicDefence,
                Element = mate.NpcMonster.Element,
                FireResistance = mate.NpcMonster.FireResistance,
                WaterResistance = mate.NpcMonster.WaterResistance,
                LightResistance = mate.NpcMonster.LightResistance,
                DarkResistance = mate.NpcMonster.DarkResistance,
                Hp = mate.Hp,
                MaxHp = mate.MaxHp,
                Mp = mate.Mp,
                MaxMp = mate.MaxMp,
                IsTeamMember = mate.IsTeamMember,
                XpLoad = mate.XpLoad,
                CanPickUp = mate.CanPickUp,
                Name = mate.DisplayName(language),
                IsSummonable = mate.IsSummonable
            };
        }

        public static ScnPacket GenerateScn(this Mate mate, RegionType language)
        {
            return new ScnPacket
            {
                PetId = mate.PetSlot,
                NpcMonsterVNum = mate.VNum,
                TransportId = mate.MateTransportId,
                Level = mate.Level,
                Loyalty = mate.Loyalty,
                Experience = mate.Experience,
                WeaponInstanceDetails = EmptySlot,
                ArmorInstanceDetails = EmptySlot,
                GauntletInstanceDetails = EmptySlot,
                BootsInstanceDetails = EmptySlot,
                AttackUpgrade = mate.NpcMonster.AttackUpgrade,
                MinimumAttack = mate.NpcMonster.DamageMinimum,
                MaximumAttack = mate.NpcMonster.DamageMaximum,
                Precision = mate.NpcMonster.Concentrate,
                CriticalRate = mate.NpcMonster.CriticalChance,
                CriticalDamageRate = mate.NpcMonster.CriticalRate,
                DefenceUpgrade = mate.NpcMonster.DefenceUpgrade,
                Defence = mate.NpcMonster.CloseDefence,
                DefenceDodge = mate.NpcMonster.DefenceDodge,
                DistanceDefence = mate.NpcMonster.DistanceDefence,
                DistanceDodge = mate.NpcMonster.DistanceDefenceDodge,
                DodgeRate = mate.NpcMonster.MagicDefence,
                ElementRate = mate.NpcMonster.Element,
                FireResistance = mate.NpcMonster.FireResistance,
                WaterResistance = mate.NpcMonster.WaterResistance,
                LightResistance = mate.NpcMonster.LightResistance,
                DarkResistance = mate.NpcMonster.DarkResistance,
                Hp = mate.Hp,
                HpMax = mate.MaxHp,
                Mp = mate.Mp,
                MpMax = mate.MaxMp,
                IsTeamMember = mate.IsTeamMember,
                LevelXp = (int)mate.XpLoad,
                Name = mate.DisplayName(language),
                MorphId = mate.Skin != 0 ? mate.Skin : -1,
                IsSummonable = mate.IsSummonable,
                SpDetails = null,
                Skill1Details = null,
                Skill2Details = null,
                Skill3Details = null
            };
        }

        /// <summary>
        /// in 2 1506 445562 26 26 2 100 100 0 0 3 626114 1 0 -1 Ratufu^pirate^(Feu)
        /// </summary>
        public static InPacket GenerateIn(this Mate mate, RegionType language)
        {
            return new InPacket
            {
                VisualType = VisualType.Npc,
                VNum = mate.VNum.ToString(CultureInfo.InvariantCulture),
                VisualId = mate.MateTransportId,
                PositionX = mate.PositionX,
                PositionY = mate.PositionY,
                Direction = mate.Direction,
                InNonPlayerSubPacket = new InNonPlayerSubPacket
                {
                    InAliveSubPacket = new InAliveSubPacket
                    {
                        Hp = Percent(mate.Hp, mate.MaxHp, 100),
                        Mp = Percent(mate.Mp, mate.MaxMp, 100)
                    },
                    Dialog = 0,
                    Faction = 0,
                    GroupEffect = 3,
                    Owner = mate.CharacterId,
                    SpawnEffect = SpawnEffectType.NoEffect,
                    IsSitting = false,
                    Morph = (short?)(mate.Skin != 0 ? mate.Skin : -1),
                    Name = mate.DisplayName(language),
                    Unknow1 = (byte)(mate.MateType == MateType.Partner ? 1 : 0)
                }
            };
        }

        public static OutPacket GenerateOut(this Mate mate)
        {
            return new OutPacket
            {
                VisualType = VisualType.Npc,
                VisualId = mate.MateTransportId
            };
        }

        /// <summary>
        /// GroupOrder carries the mate type, not a party position: pst 2 22687 0 100 100 ...
        /// </summary>
        public static PstPacket GeneratePst(this Mate mate)
        {
            return new PstPacket
            {
                Type = VisualType.Npc,
                VisualId = mate.MateTransportId,
                GroupOrder = (int)mate.MateType,
                HpLeft = Percent(mate.Hp, mate.MaxHp, 0),
                MpLeft = Percent(mate.Mp, mate.MaxMp, 0),
                HpLoad = mate.MaxHp,
                MpLoad = mate.MaxMp,
                Race = 0,
                Gender = GenderType.Male,
                Morph = 0,
                BuffIds = null
            };
        }

        public static CondPacket GenerateCond(this Mate mate)
        {
            return new CondPacket
            {
                VisualType = VisualType.Npc,
                VisualId = mate.MateTransportId,
                NoAttack = false,
                NoMove = false,
                Speed = mate.NpcMonster.Speed
            };
        }

        private static string DisplayName(this Mate mate, RegionType language)
        {
            if (!string.IsNullOrEmpty(mate.Name))
            {
                return mate.Name;
            }

            return mate.NpcMonster.Name.TryGetValue(language, out var localized)
                ? localized
                : mate.NpcMonster.Name[RegionType.EN];
        }

        private static int Percent(int current, int maximum, int whenUnknown) =>
            maximum > 0 ? (int)(current / (float)maximum * 100) : whenUnknown;

        private static ScnPacket.ScEquipmentDetails EmptySlot => new()
        {
            ItemId = -1,
            ItemRare = 0,
            ItemUpgrade = 0
        };
    }
}
