//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Networking;
using NosCore.Shared.Enumerations;
using System.Globalization;
using System.Threading.Tasks;

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
                DamageMinimum = mate.DamageMinimum,
                DamageMaximum = mate.DamageMaximum,
                Concentrate = mate.Concentrate,
                CriticalChance = mate.NpcMonster.CriticalChance,
                CriticalRate = mate.NpcMonster.CriticalRate,
                DefenceUpgrade = mate.NpcMonster.DefenceUpgrade,
                CloseDefence = mate.CloseDefence,
                DefenceDodge = mate.Dodge,
                DistanceDefence = mate.DistanceDefence,
                DistanceDefenceDodge = mate.Dodge,
                MagicDefence = mate.MagicDefence,
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

        public static NoscorePocketType? EquipmentPocket(this Mate mate) =>
            mate.MateType == MateType.Partner && mate.PetSlot < 3
                ? (NoscorePocketType)((byte)NoscorePocketType.FirstPartnerInventory + mate.PetSlot)
                : null;

        /// <summary>The partner specialist card the partner wears, whether or not it is transformed.</summary>
        public static SpecialistInstance? Specialist(this Mate mate, IInventoryService inventory)
        {
            var pocket = mate.EquipmentPocket();
            return pocket == null
                ? null
                : inventory.LoadBySlotAndType((short)EquipmentType.Sp, pocket.Value)?.ItemInstance as SpecialistInstance;
        }

        public static short Morph(this Mate mate) => mate.SpecialistMorph ?? (mate.Skin != 0 ? mate.Skin : (short)-1);

        public static ScnPacket GenerateScn(this Mate mate, RegionType language, IInventoryService inventory)
        {
            var specialist = mate.Specialist(inventory);
            return new ScnPacket
            {
                PetId = mate.PetSlot,
                NpcMonsterVNum = mate.VNum,
                TransportId = mate.MateTransportId,
                Level = mate.Level,
                Loyalty = mate.Loyalty,
                Experience = mate.Experience,
                WeaponInstanceDetails = mate.Gear(inventory, EquipmentType.MainWeapon),
                ArmorInstanceDetails = mate.Gear(inventory, EquipmentType.Armor),
                GauntletInstanceDetails = mate.Gear(inventory, EquipmentType.Gloves),
                BootsInstanceDetails = mate.Gear(inventory, EquipmentType.Boots),
                AttackUpgrade = mate.NpcMonster.AttackUpgrade,
                MinimumAttack = mate.DamageMinimum,
                MaximumAttack = mate.DamageMaximum,
                Precision = mate.Concentrate,
                CriticalRate = mate.NpcMonster.CriticalChance,
                CriticalDamageRate = mate.NpcMonster.CriticalRate,
                DefenceUpgrade = mate.NpcMonster.DefenceUpgrade,
                Defence = mate.CloseDefence,
                DefenceDodge = mate.Dodge,
                DistanceDefence = mate.DistanceDefence,
                DistanceDodge = mate.Dodge,
                DodgeRate = mate.MagicDefence,
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
                MorphId = mate.Morph(),
                IsSummonable = mate.IsSummonable,
                SpDetails = specialist is null ? null : new ScnPacket.ScSpDetails { ItemId = specialist.ItemVNum },
                Skill1Details = null,
                Skill2Details = null,
                Skill3Details = null,
                Unknown6 = specialist is null ? null : (byte)0
            };
        }

        public static InPacket GenerateIn(this Mate mate, RegionType language, short x, short y)
        {
            return new InPacket
            {
                VisualType = VisualType.Npc,
                VNum = mate.VNum.ToString(CultureInfo.InvariantCulture),
                VisualId = mate.MateTransportId,
                PositionX = x,
                PositionY = y,
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
                    Morph = mate.Morph(),
                    Name = mate.DisplayName(language),
                    Unknow1 = (byte)(mate.MateType == MateType.Partner ? 1 : 0)
                }
            };
        }

        public static PinitSubPacket GenerateSubPinit(this Mate mate, RegionType language)
        {
            return new PinitSubPacket
            {
                VisualType = VisualType.Npc,
                VisualId = mate.MateTransportId,
                GroupPosition = (int)mate.MateType,
                Level = mate.Level,
                Name = mate.DisplayName(language),
                Unknown = -1,
                Unknown2 = mate.VNum
            };
        }

        // A level gained refills the mate, and its entity on the map when it is out.
        public static Ecs.MateComponentBundle? RefillAfterLevelUp(this Mate mate)
        {
            mate.Hp = mate.MaxHp;
            mate.Mp = mate.MaxMp;
            if (mate.Entity is not { } entity)
            {
                return null;
            }

            entity.Level = mate.Level;
            entity.MaxHp = mate.MaxHp;
            entity.MaxMp = mate.MaxMp;
            entity.Hp = mate.Hp;
            entity.Mp = mate.Mp;
            return entity;
        }

        public static Task SpawnAsync(this Mate mate, PlayerComponentBundle owner, MapInstance mapInstance,
            short x, short y)
        {
            var handle = mapInstance.EcsWorld.CreateMate((int)mate.MateTransportId, mate, mapInstance, x, y, 2);
            mate.Entity = new MateComponentBundle(handle, mapInstance.EcsWorld) { Morph = mate.Morph() };

            // A hidden owner's pet would put the owner back on screen.
            var spawn = mate.GenerateIn(owner.AccountLanguage, x, y);
            return owner.Invisible ? owner.SendPacketAsync(spawn) : mapInstance.SendPacketAsync(spawn);
        }

        public static async Task DespawnAsync(this Mate mate, MapInstance mapInstance)
        {
            if (mate.Entity is not { } entity)
            {
                return;
            }

            await mapInstance.SendPacketAsync(mate.GenerateOut());
            mapInstance.EcsWorld.DestroyEntity(entity.Handle);
            mate.Entity = null;
        }

        public static OutPacket GenerateOut(this Mate mate)
        {
            return new OutPacket
            {
                VisualType = VisualType.Npc,
                VisualId = mate.MateTransportId
            };
        }

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

        private static ScnPacket.ScEquipmentDetails Gear(this Mate mate, IInventoryService inventory, EquipmentType slot)
        {
            var pocket = mate.EquipmentPocket();
            var worn = pocket == null ? null : inventory.LoadBySlotAndType((short)slot, pocket.Value)?.ItemInstance;
            return worn is WearableInstance wearable
                ? new ScnPacket.ScEquipmentDetails
                {
                    ItemId = wearable.ItemVNum,
                    ItemRare = wearable.Rare,
                    ItemUpgrade = wearable.Upgrade
                }
                : new ScnPacket.ScEquipmentDetails { ItemId = -1 };
        }
    }
}
