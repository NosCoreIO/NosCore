//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    public class Mate : MateDto
    {
        public NpcMonsterDto NpcMonster { get; set; } = null!;

        public long MateTransportId { get; set; }

        public byte PetSlot { get; set; }

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        public long XpLoad => MateXpTable.RequiredXp(Level, MateType);

        public ScpPacket GenerateScp(RegionType language)
        {
            return new ScpPacket
            {
                PetId = PetSlot,
                NpcMonsterVNum = VNum,
                TransportId = MateTransportId,
                Level = Level,
                Loyalty = Loyalty,
                Experience = Experience,
                Unknow1 = 0,
                AttackUpgrade = NpcMonster.AttackUpgrade,
                DamageMinimum = NpcMonster.DamageMinimum,
                DamageMaximum = NpcMonster.DamageMaximum,
                Concentrate = NpcMonster.Concentrate,
                CriticalChance = NpcMonster.CriticalChance,
                CriticalRate = NpcMonster.CriticalRate,
                DefenceUpgrade = NpcMonster.DefenceUpgrade,
                CloseDefence = NpcMonster.CloseDefence,
                DefenceDodge = NpcMonster.DefenceDodge,
                DistanceDefence = NpcMonster.DistanceDefence,
                DistanceDefenceDodge = NpcMonster.DistanceDefenceDodge,
                MagicDefence = NpcMonster.MagicDefence,
                Element = NpcMonster.Element,
                FireResistance = NpcMonster.FireResistance,
                WaterResistance = NpcMonster.WaterResistance,
                LightResistance = NpcMonster.LightResistance,
                DarkResistance = NpcMonster.DarkResistance,
                Hp = Hp,
                MaxHp = MaxHp,
                Mp = Mp,
                MaxMp = MaxMp,
                IsTeamMember = IsTeamMember,
                XpLoad = XpLoad,
                CanPickUp = CanPickUp,
                Name = DisplayName(language),
                IsSummonable = IsSummonable
            };
        }

        public ScnPacket GenerateScn(RegionType language)
        {
            return new ScnPacket
            {
                PetId = PetSlot,
                NpcMonsterVNum = VNum,
                TransportId = MateTransportId,
                Level = Level,
                Loyalty = Loyalty,
                Experience = Experience,
                WeaponInstanceDetails = EmptySlot,
                ArmorInstanceDetails = EmptySlot,
                GauntletInstanceDetails = EmptySlot,
                BootsInstanceDetails = EmptySlot,
                AttackUpgrade = NpcMonster.AttackUpgrade,
                MinimumAttack = NpcMonster.DamageMinimum,
                MaximumAttack = NpcMonster.DamageMaximum,
                Precision = NpcMonster.Concentrate,
                CriticalRate = NpcMonster.CriticalChance,
                CriticalDamageRate = NpcMonster.CriticalRate,
                DefenceUpgrade = NpcMonster.DefenceUpgrade,
                Defence = NpcMonster.CloseDefence,
                DefenceDodge = NpcMonster.DefenceDodge,
                DistanceDefence = NpcMonster.DistanceDefence,
                DistanceDodge = NpcMonster.DistanceDefenceDodge,
                DodgeRate = NpcMonster.MagicDefence,
                ElementRate = NpcMonster.Element,
                FireResistance = NpcMonster.FireResistance,
                WaterResistance = NpcMonster.WaterResistance,
                LightResistance = NpcMonster.LightResistance,
                DarkResistance = NpcMonster.DarkResistance,
                Hp = Hp,
                HpMax = MaxHp,
                Mp = Mp,
                MpMax = MaxMp,
                IsTeamMember = IsTeamMember,
                LevelXp = (int)XpLoad,
                Name = DisplayName(language),
                MorphId = Skin != 0 ? Skin : -1,
                IsSummonable = IsSummonable,
                SpDetails = null,
                Skill1Details = null,
                Skill2Details = null,
                Skill3Details = null
            };
        }

        private string DisplayName(RegionType language)
        {
            var name = Name;
            if (string.IsNullOrEmpty(name))
            {
                name = NpcMonster.Name.TryGetValue(language, out var localized)
                    ? localized
                    : NpcMonster.Name[RegionType.EN];
            }

            return name;
        }

        private static ScnPacket.ScEquipmentDetails EmptySlot => new()
        {
            ItemId = -1,
            ItemRare = 0,
            ItemUpgrade = 0
        };
    }
}
