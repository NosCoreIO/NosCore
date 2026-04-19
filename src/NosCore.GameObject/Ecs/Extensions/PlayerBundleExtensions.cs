//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Movement;
using NosCore.Data.Enumerations.Buff;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Ecs.Extensions;

public static class PlayerBundleExtensions
{
    public static Task SendPacketAsync(this PlayerComponentBundle player, IPacket? packet)
    {
        return player.Sender?.SendPacketAsync(packet) ?? Task.CompletedTask;
    }

    public static Task SendPacketsAsync(this PlayerComponentBundle player, IEnumerable<IPacket?> packets)
    {
        return player.Sender?.SendPacketsAsync(packets) ?? Task.CompletedTask;
    }

    public static async Task RestAsync(this PlayerComponentBundle player)
    {
        player.IsSitting = !player.IsSitting;
        await player.MapInstance.SendPacketAsync(player.GenerateRest());
    }

    public static TalkPacket GenerateTalk(this PlayerComponentBundle player, string message)
    {
        return new TalkPacket
        {
            CharacterId = player.VisualId,
            Message = message
        };
    }

    public static SpeakPacket GenerateSpk(this PlayerComponentBundle player, SpeakPacket speakPacket)
    {
        return new SpeakPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            SpeakType = speakPacket.SpeakType,
            Message = speakPacket.Message,
            EntityName = player.Name
        };
    }

    public static SayPacket GenerateSay(this PlayerComponentBundle player, SayPacket sayPacket)
    {
        return new SayPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Type = sayPacket.Type,
            Message = sayPacket.Message
        };
    }

    public static async Task<FinitPacket> GenerateFinitAsync(this PlayerComponentBundle player, IFriendHub friendHub, IChannelHub channelHub, IPubSubHub pubSubHub)
    {
        var friends = await friendHub.GetFriendsAsync(player.CharacterId);
        var friendSubPackets = new List<FinitSubPacket?>();

        foreach (var relation in friends)
        {
            var isOnline = (await pubSubHub.GetSubscribersAsync())
                .Any(s => s.ConnectedCharacter?.Id == relation.CharacterId);

            friendSubPackets.Add(new FinitSubPacket
            {
                CharacterId = relation.CharacterId,
                RelationType = relation.RelationType,
                IsOnline = isOnline,
                CharacterName = relation.CharacterName
            });
        }

        return new FinitPacket
        {
            SubPackets = friendSubPackets
        };
    }

    public static async Task<BlinitPacket> GenerateBlinitAsync(this PlayerComponentBundle player, IBlacklistHub blacklistHub)
    {
        var subpackets = new List<BlinitSubPacket?>();
        var blacklist = await blacklistHub.GetBlacklistedAsync(player.CharacterId);
        foreach (var b in blacklist)
        {
            subpackets.Add(new BlinitSubPacket
            {
                RelatedCharacterId = b.CharacterId,
                CharacterName = b.CharacterName
            });
        }
        return new BlinitPacket { SubPackets = subpackets };
    }

    public static async Task SetReputationAsync(this PlayerComponentBundle player, long reputation)
    {
        player.Reputation = reputation;
        await player.SendPacketAsync(player.GenerateFd());
        await player.MapInstance.SendPacketAsync(player.GenerateIn(""));
    }

    public static async Task SetLevelAsync(this PlayerComponentBundle player, byte level,
        IExperienceService experienceService, IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService)
    {
        player.Level = level;
        player.LevelXp = 0;
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        await player.SendPacketAsync(player.GenerateStat());
        await player.SendPacketAsync(player.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
        await player.MapInstance.SendPacketAsync(player.GenerateIn(""));
        await player.MapInstance.SendPacketAsync(player.GenerateEff(6));
        await player.MapInstance.SendPacketAsync(player.GenerateEff(198));
    }

    public static InPacket GenerateIn(this PlayerComponentBundle player, string prefix)
    {
        return new InPacket
        {
            VisualType = VisualType.Player,
            Name = prefix + player.Name,
            VisualId = player.VisualId,
            PositionX = player.PositionX,
            PositionY = player.PositionY,
            Direction = player.Direction,
            InCharacterSubPacket = new InCharacterSubPacket
            {
                Authority = player.Authority,
                Gender = player.Gender,
                HairStyle = player.HairStyle,
                HairColor = player.HairColor,
                Class = player.Class,
                Equipment = player.GetEquipmentSubPacket(),
                InAliveSubPacket = new InAliveSubPacket
                {
                    Hp = player.MaxHp > 0 ? (int)(player.Hp / (float)player.MaxHp * 100) : 100,
                    Mp = player.MaxMp > 0 ? (int)(player.Mp / (float)player.MaxMp * 100) : 100
                },
                IsSitting = player.IsSitting,
                GroupId = player.Group?.GroupId ?? -1,
                Fairy = 0,
                FairyElement = 0,
                Unknown = 0,
                Morph = 0,
                Unknown2 = 0,
                Unknown3 = 0,
                WeaponUpgradeRareSubPacket = player.GetWeaponUpgradeRareSubPacket(),
                ArmorUpgradeRareSubPacket = player.GetArmorUpgradeRareSubPacket(),
                FamilySubPacket = new FamilySubPacket(),
                FamilyName = null,
                ReputIco = (byte)(GetDignityIcon(player.Dignity) == 0 ? GetReputationIcon(player.Reputation) : -GetDignityIcon(player.Dignity)),
                Invisible = player.Invisible,
                MorphUpgrade = player.MorphUpgrade,
                Faction = 0,
                MorphUpgrade2 = (byte)player.MorphDesign,
                Level = player.Level,
                FamilyLevel = 0,
                FamilyIcons = new List<bool> { false, false, false },
                ArenaWinner = false,
                Compliment = (short)player.Compliment,
                Size = player.Size,
                HeroLevel = player.HeroLevel
            }
        };
    }

    public static InEquipmentSubPacket GetEquipmentSubPacket(this PlayerComponentBundle player)
    {
        var inventory = player.InventoryService;
        return new InEquipmentSubPacket
        {
            Armor = inventory.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            CostumeHat = inventory.LoadBySlotAndType((short)EquipmentType.CostumeHat, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            CostumeSuit = inventory.LoadBySlotAndType((short)EquipmentType.CostumeSuit, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            Fairy = inventory.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            Hat = inventory.LoadBySlotAndType((short)EquipmentType.Hat, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            MainWeapon = inventory.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            Mask = inventory.LoadBySlotAndType((short)EquipmentType.Mask, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            SecondaryWeapon = inventory.LoadBySlotAndType((short)EquipmentType.SecondaryWeapon, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            WeaponSkin = inventory.LoadBySlotAndType((short)EquipmentType.WeaponSkin, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            WingSkin = inventory.LoadBySlotAndType((short)EquipmentType.WingSkin, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum
        };
    }

    public static UpgradeRareSubPacket GetWeaponUpgradeRareSubPacket(this PlayerComponentBundle player)
    {
        var weapon = player.InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear);
        return new UpgradeRareSubPacket
        {
            Upgrade = weapon?.ItemInstance?.Upgrade ?? 0,
            Rare = (sbyte)(weapon?.ItemInstance?.Rare ?? 0)
        };
    }

    public static UpgradeRareSubPacket GetArmorUpgradeRareSubPacket(this PlayerComponentBundle player)
    {
        var armor = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear);
        return new UpgradeRareSubPacket
        {
            Upgrade = armor?.ItemInstance?.Upgrade ?? 0,
            Rare = (sbyte)(armor?.ItemInstance?.Rare ?? 0)
        };
    }

    public static StatPacket GenerateStat(this PlayerComponentBundle player)
    {
        return new StatPacket
        {
            Hp = player.Hp,
            HpMaximum = player.MaxHp,
            Mp = player.Mp,
            MpMaximum = player.MaxMp,
            Unknown = 0,
            Option = 0
        };
    }

    public static GoldPacket GenerateGold(this PlayerComponentBundle player)
    {
        return new GoldPacket { Gold = player.Gold };
    }

    public static LevPacket GenerateLev(this PlayerComponentBundle player,
        IExperienceService experienceService,
        IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService)
    {
        return new LevPacket
        {
            Level = player.Level,
            LevelXp = player.LevelXp,
            JobLevel = player.JobLevel,
            JobLevelXp = player.JobLevelXp,
            XpLoad = experienceService.GetExperience(player.Level),
            JobXpLoad = jobExperienceService.GetJobExperience(player.Class, player.JobLevel),
            Reputation = player.Reputation,
            SkillCp = 0,
            HeroXp = player.HeroLevelXp,
            HeroLevel = player.HeroLevel,
            HeroXpLoad = player.HeroLevel == 0 ? 0 : heroExperienceService.GetHeroExperience(player.HeroLevel)
        };
    }

    public static FdPacket GenerateFd(this PlayerComponentBundle player)
    {
        return new FdPacket
        {
            Reput = player.Reputation,
            ReputIcon = (int)GetReputationIcon(player.Reputation),
            Dignity = player.Dignity,
            DignityIcon = (int)GetDignityIcon(player.Dignity)
        };
    }

    public static AtPacket GenerateAt(this PlayerComponentBundle player, short mapId, int music = 0)
    {
        return new AtPacket
        {
            CharacterId = player.VisualId,
            MapId = mapId,
            PositionX = player.PositionX,
            PositionY = player.PositionY,
            Direction = player.Direction,
            Unknown1 = 0,
            Music = music,
            Unknown2 = 0,
            Unknown3 = -1
        };
    }

    public static CInfoPacket GenerateCInfo(this PlayerComponentBundle player)
    {
        return new CInfoPacket
        {
            Name = player.Name,
            Unknown1 = null,
            GroupId = -1,
            FamilyId = -1,
            FamilyName = null,
            CharacterId = player.VisualId,
            Authority = player.Authority,
            Gender = player.Gender,
            HairStyle = player.HairStyle,
            HairColor = player.HairColor,
            Class = player.Class,
            Icon = (byte)(GetDignityIcon(player.Dignity) == 0 ? GetReputationIcon(player.Reputation) : -GetDignityIcon(player.Dignity)),
            Compliment = (short)player.Compliment,
            Morph = player.Morph,
            Invisible = player.Invisible,
            FamilyLevel = 0,
            MorphUpgrade = player.MorphUpgrade,
            ArenaWinner = false
        };
    }

    public static CondPacket GenerateCond(this PlayerComponentBundle player)
    {
        return new CondPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            NoAttack = player.NoAttack,
            NoMove = player.NoMove,
            Speed = player.Speed
        };
    }

    public static CModePacket GenerateCMode(this PlayerComponentBundle player)
    {
        return new CModePacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Morph = player.Morph,
            MorphUpgrade = player.MorphUpgrade,
            MorphDesign = player.MorphDesign,
            MorphBonus = player.MorphBonus,
            Size = player.Size
        };
    }

    public static OutPacket GenerateOut(this PlayerComponentBundle player)
    {
        return new OutPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId
        };
    }

    public static RestPacket GenerateRest(this PlayerComponentBundle player)
    {
        return new RestPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            IsSitting = player.IsSitting
        };
    }

    private static int GetReputationIcon(long reputation)
    {
        return reputation switch
        {
            >= 5000001 => 28,
            >= 2500001 => 27,
            >= 500001 => 26,
            >= 250001 => 25,
            >= 100001 => 24,
            >= 50001 => 23,
            >= 10001 => 22,
            >= 5001 => 21,
            >= 2501 => 20,
            >= 501 => 19,
            >= 251 => 18,
            >= 1 => 17,
            _ => 16
        };
    }

    private static int GetDignityIcon(int dignity)
    {
        return dignity switch
        {
            < -1000 => 1,
            <= -800 => 2,
            <= -600 => 3,
            <= -400 => 4,
            <= -200 => 5,
            <= -100 => 6,
            _ => 7
        };
    }

    public static EffectPacket GenerateEff(this PlayerComponentBundle player, int effectId)
    {
        return new EffectPacket
        {
            EffectType = VisualType.Player,
            VisualEntityId = player.VisualId,
            Id = effectId
        };
    }

    public static SayPacket GenerateSay(this PlayerComponentBundle player, string message, SayColorType type)
    {
        return new SayPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Type = type,
            Message = message
        };
    }

    public static UseItemPacket GenerateUseItem(this PlayerComponentBundle player, PocketType type, short slot, byte mode, byte parameter)
    {
        return new UseItemPacket
        {
            VisualId = player.VisualId,
            VisualType = VisualType.Player,
            Type = type,
            Slot = slot,
            Mode = mode,
            Parameter = parameter
        };
    }

    public static EqPacket GenerateEq(this PlayerComponentBundle player)
    {
        return new EqPacket
        {
            VisualId = player.VisualId,
            Visibility = (byte)(player.Authority < AuthorityType.GameMaster ? 0 : 2),
            Gender = player.Gender,
            HairStyle = player.HairStyle,
            Haircolor = player.HairColor,
            ClassType = player.Class,
            EqSubPacket = player.GetEquipmentSubPacket(),
            WeaponUpgradeRarePacket = player.GetWeaponUpgradeRareSubPacket(),
            ArmorUpgradeRarePacket = player.GetArmorUpgradeRareSubPacket(),
            Size = player.Size
        };
    }

    public static EquipPacket GenerateEquipment(this PlayerComponentBundle player)
    {
        var inventory = player.InventoryService;

        EquipmentSubPacket? GenerateEquipmentSubPacket(EquipmentType eqType)
        {
            var eq = inventory.LoadBySlotAndType((short)eqType, NoscorePocketType.Wear);
            if (eq == null)
            {
                return null;
            }

            return new EquipmentSubPacket
            {
                EquipmentType = eqType,
                VNum = eq.ItemInstance.ItemVNum,
                Rare = eq.ItemInstance.Rare,
                Upgrade = (eq.ItemInstance.Item.IsColored ? eq.ItemInstance?.Design
                    : eq.ItemInstance.Upgrade) ?? 0,
                Unknown = 0
            };
        }

        return new EquipPacket
        {
            WeaponUpgradeRareSubPacket = player.GetWeaponUpgradeRareSubPacket(),
            ArmorUpgradeRareSubPacket = player.GetArmorUpgradeRareSubPacket(),
            Armor = GenerateEquipmentSubPacket(EquipmentType.Armor),
            WeaponSkin = GenerateEquipmentSubPacket(EquipmentType.WeaponSkin),
            SecondaryWeapon = GenerateEquipmentSubPacket(EquipmentType.SecondaryWeapon),
            Sp = GenerateEquipmentSubPacket(EquipmentType.Sp),
            Amulet = GenerateEquipmentSubPacket(EquipmentType.Amulet),
            Boots = GenerateEquipmentSubPacket(EquipmentType.Boots),
            CostumeHat = GenerateEquipmentSubPacket(EquipmentType.CostumeHat),
            CostumeSuit = GenerateEquipmentSubPacket(EquipmentType.CostumeSuit),
            Fairy = GenerateEquipmentSubPacket(EquipmentType.Fairy),
            Gloves = GenerateEquipmentSubPacket(EquipmentType.Gloves),
            Hat = GenerateEquipmentSubPacket(EquipmentType.Hat),
            MainWeapon = GenerateEquipmentSubPacket(EquipmentType.MainWeapon),
            Mask = GenerateEquipmentSubPacket(EquipmentType.Mask),
            Necklace = GenerateEquipmentSubPacket(EquipmentType.Necklace),
            Ring = GenerateEquipmentSubPacket(EquipmentType.Ring),
            Bracelet = GenerateEquipmentSubPacket(EquipmentType.Bracelet),
            WingSkin = GenerateEquipmentSubPacket(EquipmentType.WingSkin)
        };
    }

    public static SpPacket GenerateSpPoint(this PlayerComponentBundle player, IOptions<WorldConfiguration> worldConfiguration)
    {
        return new SpPacket
        {
            AdditionalPoint = player.SpAdditionPoint,
            MaxAdditionalPoint = worldConfiguration.Value.MaxAdditionalSpPoints,
            SpPoint = player.SpPoint,
            MaxSpPoint = worldConfiguration.Value.MaxSpPoints
        };
    }

    public static TitleInfoPacket GenerateTitInfo(this PlayerComponentBundle player)
    {
        var visibleTitle = player.Titles.FirstOrDefault(s => s.Visible)?.TitleType;
        var effectiveTitle = player.Titles.FirstOrDefault(s => s.Active)?.TitleType;
        return new TitleInfoPacket
        {
            VisualId = player.VisualId,
            EffectiveTitle = effectiveTitle ?? 0,
            VisualType = VisualType.Player,
            VisibleTitle = visibleTitle ?? 0
        };
    }

    public static PairyPacket GeneratePairy(this PlayerComponentBundle player, WearableInstance? fairy)
    {
        var isBuffed = false;
        return new PairyPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            FairyMoveType = fairy == null ? 0 : 4,
            Element = fairy?.Item?.Element ?? 0,
            ElementRate = fairy?.ElementRate + fairy?.Item?.ElementRate ?? 0,
            Morph = fairy?.Item?.Morph ?? 0 + (isBuffed ? 5 : 0)
        };
    }

    public static TitlePacket GenerateTitle(this PlayerComponentBundle player)
    {
        var data = player.Titles.Select(s => new TitleSubPacket
        {
            TitleId = (short)(s.TitleType - 9300),
            TitleStatus = (byte)((s.Visible ? 2 : 0) + (s.Active ? 4 : 0) + 1)
        }).ToList() as List<TitleSubPacket?>;
        return new TitlePacket
        {
            Data = data.Any() ? data : null
        };
    }

    public static IconPacket GenerateIcon(this PlayerComponentBundle player, byte iconType, short iconParameter)
    {
        return new IconPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            IconParameter = iconParameter,
            IconType = iconType
        };
    }

    public static ServerGetPacket GenerateGet(this PlayerComponentBundle player, long itemId)
    {
        return new ServerGetPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            ItemId = itemId
        };
    }

    public static MlobjlstPacket GenerateMlobjlst(this PlayerComponentBundle player)
    {
        var mlobj = new List<MlobjlstSubPacket?>();
        foreach (var item in player.InventoryService.Where(s => s.Value.Type == NoscorePocketType.Miniland)
            .OrderBy(s => s.Value.Slot).Select(s => s.Value))
        {
            var used = player.MapInstance.MapDesignObjects.ContainsKey(item.Id);
            var mp = used ? player.MapInstance.MapDesignObjects[item.Id] : null;

            mlobj.Add(new MlobjlstSubPacket
            {
                InUse = used,
                Slot = item.Slot,
                MlObjSubPacket = new MlobjSubPacket
                {
                    MapX = used ? mp!.MapX : (short)0,
                    MapY = used ? mp!.MapY : (short)0,
                    Width = item.ItemInstance.Item.Width != 0 ? item.ItemInstance.Item.Width : (byte)1,
                    Height = item.ItemInstance.Item.Height != 0 ? item.ItemInstance.Item.Height : (byte)1,
                    DurabilityPoint = used ? item.ItemInstance.DurabilityPoint : 0,
                    Unknown = 100,
                    Unknown2 = false,
                    IsWarehouse = item.ItemInstance.Item.IsWarehouse
                }
            });
        }

        return new MlobjlstPacket
        {
            MlobjlstSubPacket = mlobj
        };
    }

    public static SayItemPacket GenerateSayItem(this PlayerComponentBundle player, string message, InventoryItemInstance item)
    {
        var isNormalItem = item.Type != NoscorePocketType.Equipment && item.Type != NoscorePocketType.Specialist;
        return new SayItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            OratorSlot = 17,
            Message = message,
            IconInfo = isNormalItem ? new IconInfoPacket
            {
                IconId = item.ItemInstance.ItemVNum
            } : null,
            EquipmentInfo = isNormalItem ? null : new EInfoPacket(),
            SlInfo = item.Type != NoscorePocketType.Specialist ? null : new SlInfoPacket()
        };
    }

    public static StPacket GenerateStatInfo(this PlayerComponentBundle player)
    {
        return new StPacket
        {
            Type = VisualType.Player,
            VisualId = player.VisualId,
            Level = player.Level,
            HeroLvl = player.HeroLevel,
            HpPercentage = (int)(player.Hp / (float)player.MaxHp * 100),
            MpPercentage = (int)(player.Mp / (float)player.MaxMp * 100),
            CurrentHp = player.Hp,
            CurrentMp = player.Mp,
            BuffIds = null
        };
    }

    public static PflagPacket GeneratePFlag(this PlayerComponentBundle player)
    {
        return new PflagPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Flag = player.Shop?.ShopId ?? 0
        };
    }

    public static ShopPacket GenerateShop(this PlayerComponentBundle player, RegionType language)
    {
        return new ShopPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            ShopId = player.Shop?.ShopId ?? 0,
            MenuType = player.Shop?.MenuType ?? 0,
            ShopType = player.Shop?.ShopType,
            Name = player.Shop?.Name[language]
        };
    }

    public static DirPacket GenerateChangeDir(this PlayerComponentBundle player)
    {
        return new DirPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Direction = player.Direction
        };
    }

    public static MovePacket GenerateMove(this PlayerComponentBundle player)
    {
        return player.GenerateMove(null, null);
    }

    public static MovePacket GenerateMove(this PlayerComponentBundle player, short? mapX, short? mapY)
    {
        return new MovePacket
        {
            VisualEntityId = player.VisualId,
            MapX = mapX ?? player.PositionX,
            MapY = mapY ?? player.PositionY,
            Speed = player.Speed,
            VisualType = VisualType.Player
        };
    }

    public static PidxSubPacket GenerateSubPidx(this PlayerComponentBundle player)
    {
        return player.GenerateSubPidx(false);
    }

    public static PidxSubPacket GenerateSubPidx(this PlayerComponentBundle player, bool isMemberOfGroup)
    {
        return new PidxSubPacket
        {
            IsGrouped = isMemberOfGroup,
            VisualId = player.VisualId
        };
    }

    public static PinitSubPacket GenerateSubPinit(this PlayerComponentBundle player, int groupPosition)
    {
        return new PinitSubPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            GroupPosition = groupPosition,
            Level = player.Level,
            Name = player.Name,
            Gender = player.Gender,
            Race = 0,
            Morph = player.Morph,
            HeroLevel = player.HeroLevel
        };
    }

    public static void LoadExpensions(this PlayerComponentBundle player)
    {
        var backpack = player.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BackPack);
        var backpackticket = player.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.InventoryTicketUpgrade);
        var expension = (byte)((backpack ? 12 : 0) + (backpackticket ? 60 : 0));

        player.InventoryService.Expensions[NoscorePocketType.Main] += expension;
        player.InventoryService.Expensions[NoscorePocketType.Equipment] += expension;
        player.InventoryService.Expensions[NoscorePocketType.Etc] += expension;
    }

    public static ExtsPacket GenerateExts(this PlayerComponentBundle player, IOptions<WorldConfiguration> conf)
    {
        return new ExtsPacket
        {
            EquipmentExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Equipment] + conf.Value.BackpackSize),
            MainExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Main] + conf.Value.BackpackSize),
            EtcExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Etc] + conf.Value.BackpackSize)
        };
    }

    public static void AddSpPoints(this PlayerComponentBundle player, int spPointToAdd, IOptions<WorldConfiguration> worldConfiguration)
    {
        var newValue = player.SpPoint + spPointToAdd;
        player.SpPoint = newValue > worldConfiguration.Value.MaxSpPoints
            ? worldConfiguration.Value.MaxSpPoints : newValue;
    }

    public static void AddAdditionalSpPoints(this PlayerComponentBundle player, int spPointToAdd, IOptions<WorldConfiguration> worldConfiguration)
    {
        var newValue = player.SpAdditionPoint + spPointToAdd;
        player.SpAdditionPoint = newValue > worldConfiguration.Value.MaxAdditionalSpPoints
            ? worldConfiguration.Value.MaxAdditionalSpPoints : newValue;
    }

    public static void RemoveGold(this PlayerComponentBundle player, long gold)
    {
        player.Gold -= gold;
    }

    public static TitPacket GenerateTit(this PlayerComponentBundle player)
    {
        return new TitPacket
        {
            ClassType = (Game18NConstString)Enum.Parse(typeof(Game18NConstString), player.Class.ToString()),
            Name = player.Name
        };
    }
}

public static class ClientSessionMailExtensions
{
    public static async Task GenerateMailAsync(this ClientSession session, IEnumerable<MailData> mails)
    {
        var playerName = session.Character.Name;
        foreach (var mail in mails)
        {
            if (!mail.MailDto.IsSenderCopy && (mail.ReceiverName == playerName))
            {
                if (mail.ItemInstance != null)
                {
                    await session.SendPacketAsync(mail.GeneratePost(0)!);
                }
                else
                {
                    await session.SendPacketAsync(mail.GeneratePost(1)!);
                }
            }
            else
            {
                if (mail.ItemInstance != null)
                {
                    await session.SendPacketAsync(mail.GeneratePost(3)!);
                }
                else
                {
                    await session.SendPacketAsync(mail.GeneratePost(2)!);
                }
            }
        }
    }

    public static async Task ChangeClassAsync(this ClientSession session, CharacterClassType classType,
        IOptions<WorldConfiguration> worldConfiguration,
        IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
    {
        var character = session.Character;
        var inventoryService = character.InventoryService;
        var characterId = character.CharacterId;
        var mapInstance = character.MapInstance;
        var itemProvider = character.ItemProvider;
        var group = character.Group;

        if (inventoryService.Any(s => s.Value.Type == NoscorePocketType.Wear))
        {
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = characterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.RemoveEquipment
            });
            return;
        }

        character = session.Character;
        character.JobLevel = 1;
        character.JobLevelXp = 0;
        await session.SendPacketAsync(new NpInfoPacket());
        await session.SendPacketAsync(new PclearPacket());

        if (classType == CharacterClassType.Adventurer)
        {
            character = session.Character;
            var currentHairStyle = character.HairStyle;
            character.HairStyle = currentHairStyle > HairStyleType.HairStyleB ? 0 : currentHairStyle;
        }

        character = session.Character;
        character.Class = classType;
        character.Hp = character.MaxHp;
        character.Mp = character.MaxMp;

        var itemsToAdd = new List<BasicEquipment>();
        foreach (var (key, _) in worldConfiguration.Value.BasicEquipments)
        {
            switch (key)
            {
                case nameof(CharacterClassType.Adventurer) when classType == CharacterClassType.Adventurer:
                case nameof(CharacterClassType.Archer) when classType == CharacterClassType.Archer:
                case nameof(CharacterClassType.Mage) when classType == CharacterClassType.Mage:
                case nameof(CharacterClassType.MartialArtist) when classType == CharacterClassType.MartialArtist:
                case nameof(CharacterClassType.Swordsman) when classType == CharacterClassType.Swordsman:
                    itemsToAdd.AddRange(worldConfiguration.Value.BasicEquipments[key]);
                    break;
            }
        }

        foreach (var itemToAdd in itemsToAdd)
        {
            var inv = inventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemProvider.Create(itemToAdd.VNum, itemToAdd.Amount), characterId),
                itemToAdd.NoscorePocketType);
            if (inv != null)
            {
                await session.SendPacketsAsync(
                    inv.Select(invItem => invItem.GeneratePocketChange((PocketType)invItem.Type, invItem.Slot)));
            }
        }

        character = session.Character;
        var titPacket = character.GenerateTit();
        var statPacket = character.GenerateStat();
        var eqPacket = character.GenerateEq();
        var effPacket8 = character.GenerateEff(8);
        var condPacket = character.GenerateCond();
        var levPacket = character.GenerateLev(experienceService, jobExperienceService, heroExperienceService);
        var cmodePacket = character.GenerateCMode();
        var msgiPacket = new MsgiPacket
        {
            Type = MessageType.Default,
            Message = Game18NConstString.ClassChanged
        };

        await session.SendPacketAsync(titPacket);
        await session.SendPacketAsync(statPacket);
        await mapInstance.SendPacketAsync(eqPacket);
        await mapInstance.SendPacketAsync(effPacket8);
        await session.SendPacketAsync(condPacket);
        await session.SendPacketAsync(levPacket);
        await session.SendPacketAsync(cmodePacket);
        await session.SendPacketAsync(msgiPacket);

        character = session.Character;
        character.QuicklistEntries = new List<QuicklistEntryDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                QuickListIndex = 0,
                Slot = 9,
                Type = 1,
                IconType = 3,
                IconVNum = 1
            }
        };

        character = session.Character;
        var channelId = session.Channel!.Id;
        var inPacket = character.GenerateIn("");
        var pidxPacket = group!.GeneratePidx(character);
        var effPacket6 = character.GenerateEff(6);
        var effPacket198 = character.GenerateEff(198);

        await mapInstance.SendPacketAsync(inPacket, new EveryoneBut(channelId));
        await mapInstance.SendPacketAsync(pidxPacket);
        await mapInstance.SendPacketAsync(effPacket6);
        await mapInstance.SendPacketAsync(effPacket198);
    }
}
