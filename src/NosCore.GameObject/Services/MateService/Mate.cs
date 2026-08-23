//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Visibility;
using System.Globalization;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    public class Mate : MateDto
    {
        public NpcMonsterDto NpcMonster { get; set; } = null!;

        public long MateTransportId { get; set; }

        public byte PetSlot { get; set; }

        /// <summary>
        /// Where the mate is standing right now, which is not where it was stored. MapX and MapY
        /// are the square it was last saved on; these two move with the owner.
        /// </summary>
        public short PositionX { get; set; }

        /// <inheritdoc cref="PositionX" />
        public short PositionY { get; set; }

        /// <summary>
        /// The mate's place in the world while it is out, or null while it is not.
        /// </summary>
        /// <remarks>
        /// A mate that can be hit has to be an entity like any other combatant — the battle
        /// service asks for an Arch handle, and giving mates a second notion of "thing that
        /// fights" would mean maintaining two. The handle lives here rather than in a registry
        /// because the mate is already the thing everyone holds.
        /// </remarks>
        public Ecs.MateComponentBundle? Entity { get; set; }

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        /// <summary>
        /// Written when the mate is loaded rather than computed here: the curve lives in
        /// NosCore.Algorithm, and a data object has no business resolving a service.
        /// </summary>
        public long XpLoad { get; set; }

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

        /// <summary>
        /// The spawn packet. Owner and GroupEffect are what tell the client this is somebody's
        /// mate rather than a map npc:
        ///     in 2 1506 445562 26 26 2 100 100 0 0 3 626114 1 0 -1 Ratufu^pirate^(Feu) 0 -1 ...
        /// </summary>
        public InPacket GenerateIn(RegionType language)
        {
            return new InPacket
            {
                VisualType = VisualType.Npc,
                VNum = VNum.ToString(CultureInfo.InvariantCulture),
                VisualId = MateTransportId,
                PositionX = PositionX,
                PositionY = PositionY,
                Direction = Direction,
                InNonPlayerSubPacket = new InNonPlayerSubPacket
                {
                    InAliveSubPacket = new InAliveSubPacket
                    {
                        Hp = MaxHp > 0 ? (int)(Hp / (float)MaxHp * 100) : 100,
                        Mp = MaxMp > 0 ? (int)(Mp / (float)MaxMp * 100) : 100
                    },
                    Dialog = 0,
                    Faction = 0,
                    GroupEffect = 3,
                    Owner = CharacterId,
                    SpawnEffect = SpawnEffectType.NoEffect,
                    IsSitting = false,
                    Morph = (short?)(Skin != 0 ? Skin : -1),
                    Name = DisplayName(language),
                    Unknow1 = (byte)(MateType == MateType.Partner ? 1 : 0)
                }
            };
        }

        public OutPacket GenerateOut()
        {
            return new OutPacket
            {
                VisualType = VisualType.Npc,
                VisualId = MateTransportId
            };
        }

        /// <summary>
        /// The health bar in the party frame. GroupOrder carries the mate type here, not a
        /// position in the party:  pst 2 22687 0 100 100 24471 3100 0 0 0
        /// </summary>
        public PstPacket GeneratePst()
        {
            return new PstPacket
            {
                Type = VisualType.Npc,
                VisualId = MateTransportId,
                GroupOrder = (int)MateType,
                HpLeft = MaxHp > 0 ? (int)(Hp / (float)MaxHp * 100) : 0,
                MpLeft = MaxMp > 0 ? (int)(Mp / (float)MaxMp * 100) : 0,
                HpLoad = MaxHp,
                MpLoad = MaxMp,
                Race = 0,
                Gender = GenderType.Male,
                Morph = 0,
                BuffIds = null
            };
        }

        public CondPacket GenerateCond()
        {
            return new CondPacket
            {
                VisualType = VisualType.Npc,
                VisualId = MateTransportId,
                NoAttack = false,
                NoMove = false,
                Speed = NpcMonster.Speed
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
