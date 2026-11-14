//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Algorithm.BankService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Data.Enumerations.I18N;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Battle;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Bank;
using NosCore.Packets.ServerPackets.Battle;
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
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Ecs.Extensions;

public static class PlayerBundleExtensions
{
    public static WopenPacket GenerateBazaarWindow(this PlayerComponentBundle player, IClock clock)
    {
        var medal = player.BazaarMedal;
        return new WopenPacket
        {
            Type = WindowType.NosBazaar,
            Unknown = medal == null ? (byte)0
                : medal.StaticBonusType == StaticBonusType.BazaarMedalGold ? (byte)MedalType.Gold : (byte)MedalType.Silver,
            Unknown2 = medal == null ? (byte)0
                : (byte)(int)(medal.DateEnd == null ? 720 : (medal.DateEnd.Value - clock.GetCurrentInstant()).TotalHours),
        };
    }

    // An emptied stack is gone from the inventory, and the client then needs the slot cleared.
    public static Task UseUpOneAsync(this PlayerComponentBundle player, InventoryItemInstance item)
    {
        var left = player.InventoryService.RemoveItemAmountFromInventory(1, item.ItemInstanceId);
        return player.SendPacketAsync(left.GeneratePocketChange(item.Type.ToPocketType(), item.Slot));
    }

    // The healed amount floats over the character for everyone on the map; MP has no such number.
    public static async Task HealAsync(this PlayerComponentBundle player, int hp, int mp)
    {
        var healed = Math.Max(Math.Min(player.MaxHp - player.Hp, hp), 0);
        var restored = Math.Max(Math.Min(player.MaxMp - player.Mp, mp), 0);
        if (healed == 0 && restored == 0)
        {
            return;
        }

        player.Hp += healed;
        player.Mp += restored;
        if (healed > 0)
        {
            await player.MapInstance.SendPacketAsync(new RcPacket
            {
                VisualType = VisualType.Player,
                VisualId = player.VisualId,
                HealAmount = healed,
                Unknown = 0
            });
        }

        await player.SendPacketAsync(player.GenerateStat());
    }

    public static FsPacket GenerateFaction(this PlayerComponentBundle player) =>
        new() { Faction = player.Faction };

    public static Task SendPacketAsync(this PlayerComponentBundle player, IPacket? packet)
    {
        return player.Sender?.SendPacketAsync(packet) ?? Task.CompletedTask;
    }

    public static Task SendPacketsAsync(this PlayerComponentBundle player, IEnumerable<IPacket?> packets)
    {
        return player.Sender?.SendPacketsAsync(packets) ?? Task.CompletedTask;
    }

    // Attacking keeps a player on their feet for a while; being hit does not.
    private static readonly Duration SitLockAfterAttack = Duration.FromSeconds(3);

    public static bool CanSitDown(this PlayerComponentBundle player, Instant now) =>
        now - player.LastAttack >= SitLockAfterAttack;

    private static readonly Duration CombatWindow = Duration.FromSeconds(20);

    public static bool IsInCombat(this PlayerComponentBundle player, Instant now) =>
        now - player.LastAttack < CombatWindow || now - player.LastDamaged < CombatWindow;

    public static IEnumerable<IPacket> GenerateMateRoster(this PlayerComponentBundle player) =>
        new IPacket[] { new PclearPacket() }
            .Concat(MateService.GenerateScPackets(player.Mates.Values, player.AccountLanguage, player.InventoryService));

    public static Task RestAsync(this PlayerComponentBundle player)
    {
        if (player.IsSitting)
        {
            return player.StandUpAsync();
        }

        player.IsSitting = true;
        return player.MapInstance.SendPacketAsync(player.GenerateRest());
    }

    public static async Task StandUpAsync(this PlayerComponentBundle player)
    {
        player.IsSitting = false;

        // Cleared here as well as on the tick, or standing and sitting back down within one
        // tick would keep the meal.
        var digestion = player.Digestion;
        await digestion.Lock.WaitAsync();
        try
        {
            digestion.Food.Clear();
        }
        finally
        {
            digestion.Lock.Release();
        }

        await player.MapInstance.SendPacketAsync(player.GenerateRest());
    }

    public static BubblePacket GenerateBubble(this PlayerComponentBundle player, string message) => new()
    {
        CharacterId = player.VisualId,
        Message = message
    };

    // The bubble is only relayed while it lasts; one that stops being relayed fades in the client.
    public static BubblePacket? GenerateActiveBubble(this PlayerComponentBundle player, Instant now) =>
        player.SpeechBubble is { } message && player.SpeechBubbleExpiresAt > now
            ? player.GenerateBubble(message)
            : null;

    public static TalkPacket GenerateTalk(this PlayerComponentBundle player, string message)
    {
        return new TalkPacket
        {
            CharacterId = player.VisualId,
            Message = message
        };
    }

    public static async Task<FinitPacket> GenerateFinitAsync(this PlayerComponentBundle player, IFriendHub friendHub, IChannelHub channelHub, IPubSubHub pubSubHub)
    {
        var friends = await friendHub.GetFriendsAsync(player.CharacterId);
        var friendSubPackets = new List<FinitSubPacket?>();
        if (friends.Count > 0)
        {
            var online = (await pubSubHub.GetSubscribersAsync())
                .Select(s => s.ConnectedCharacter?.Id)
                .OfType<long>()
                .ToHashSet();

            friendSubPackets.AddRange(friends.Select(relation => new FinitSubPacket
            {
                CharacterId = relation.CharacterId,
                RelationType = relation.RelationType,
                IsOnline = online.Contains(relation.CharacterId),
                CharacterName = relation.CharacterName
            }));
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

    public static async Task SetGoldAsync(this PlayerComponentBundle player, long gold, List<ItemDto> items)
    {
        player.Gold = gold;
        await player.SendPacketAsync(player.GenerateGold());
        var goldName = items.First(i => i.Effect == ItemEffectType.Gold).Name[player.AccountLanguage];
        await player.SendPacketAsync(player.GenerateGoldReceived(gold, goldName));
    }

    public static async Task SetReputationAsync(this PlayerComponentBundle player, long reputation)
    {
        var delta = reputation - player.Reputation;
        player.Reputation = reputation;
        await player.SendPacketAsync(player.GenerateFd());
        if (delta != 0)
        {
            await player.SendPacketAsync(player.GenerateSayi(delta > 0 ? SayColorType.Green : SayColorType.Red,
                delta > 0 ? Game18NConstString.ReputationIncreased : Game18NConstString.ReputationReduced, 4, Math.Abs(delta)));
        }
        await player.MapInstance.SendPacketAsync(player.GenerateIn(""));
    }

    public static async Task SetLevelAsync(this PlayerComponentBundle player, byte level,
        IExperienceService experienceService, IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService, ISkillPointService skillPointService, ISessionRegistry sessionRegistry,
        IGameLanguageLocalizer localizer)
    {
        player.Level = level;
        player.LevelXp = 0;
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        await player.SendPacketAsync(player.GenerateStat());
        await player.SendPacketAsync(player.GenerateStatInfo());
        await player.SendPacketAsync(player.GenerateLev(experienceService, jobExperienceService, heroExperienceService, skillPointService));

        var visualId = player.VisualId;
        var authority = player.Authority;
        var supportPrefix = authority == AuthorityType.Moderator
            ? localizer[LanguageKey.SUPPORT, player.AccountLanguage] : string.Empty;
        var inPacket = player.GenerateIn(supportPrefix);
        var eff6 = player.GenerateEff(6);
        var eff198 = player.GenerateEff(198);

        var mapInstance = player.MapInstance;
        var mapSessions = sessionRegistry.GetCharacters(s => s.MapInstance == mapInstance).ToList();
        await Task.WhenAll(mapSessions.Select(async s =>
        {
            if (s.VisualId != visualId)
            {
                await s.SendPacketAsync(inPacket);
            }
            await s.SendPacketAsync(eff6);
            await s.SendPacketAsync(eff198);
        }));

        var group = player.Group;
        if (group != null)
        {
            foreach (var member in group.Keys)
            {
                if (member.Item1 != VisualType.Player)
                {
                    continue;
                }

                if (sessionRegistry.TryGetCharacter(s => s.VisualId == member.Item2, out var groupMember))
                {
                    await groupMember.SendPacketAsync(groupMember.GeneratePinit());
                }
            }
            await player.SendPacketAsync(player.GeneratePinit());
        }

        await player.SendPacketAsync(new MsgiPacket
        {
            Type = MessageType.Default,
            Message = Game18NConstString.LevelIncreased
        });
    }

    /// <summary>
    /// Builds the player's map appearance packet, including faction and visible equipment.
    /// </summary>
    public static InPacket GenerateIn(this PlayerComponentBundle player, string prefix)
    {
        var fairy = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance as WearableInstance;
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
                Authority = player.Authority >= AuthorityType.Administrator ? AuthorityType.Administrator : AuthorityType.User,
                Gender = player.Gender,
                HairStyle = player.HairStyle,
                HairColor = player.ShownHairColor(),
                Class = player.Class,
                Equipment = player.GetEquipmentSubPacket(),
                InAliveSubPacket = new InAliveSubPacket
                {
                    Hp = player.MaxHp > 0 ? (int)(player.Hp / (float)player.MaxHp * 100) : 100,
                    Mp = player.MaxMp > 0 ? (int)(player.Mp / (float)player.MaxMp * 100) : 100
                },
                IsSitting = player.IsSitting,
                GroupId = player.Group.Count == 1 ? -1 : player.Group.GroupId,
                Fairy = (byte)(fairy == null ? 0 : 4),
                FairyElement = (byte)(fairy?.Item.Element ?? 0),
                FairyBooster = false,
                FairyMorph = (byte)(fairy?.Item.Morph ?? 0),
                ShowInEffect = false,
                Morph = player.UseSp && !player.IsVehicled ? (byte)player.Morph : (byte)0,
                WeaponUpgradeRareSubPacket = player.GetWeaponUpgradeRareSubPacket(),
                ArmorUpgradeRareSubPacket = player.GetArmorUpgradeRareSubPacket(),
                FamilySubPacket = new FamilySubPacket(),
                FamilyName = null,
                ReputIco = GetIcon(player),
                Invisible = false,
                MorphUpgrade = player.UseSp ? player.MorphUpgrade : (byte)0,
                Faction = player.Faction,
                MorphUpgrade2 = player.UseSp ? (byte)player.MorphDesign : (byte)0,
                Level = player.Level,
                FamilyLevel = 0,
                FamilyIcons = new List<bool> { false, false, false },
                ArenaWinner = false,
                Compliment = (short)(player.Authority >= AuthorityType.Moderator ? 500 : player.Compliment),
                Size = player.Size,
                HeroLevel = player.HeroLevel
            }
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
            Option = player.StatOption
        };
    }

    public static GoldPacket GenerateGold(this PlayerComponentBundle player)
    {
        return new GoldPacket { Gold = player.Gold };
    }

    public static GbPacket GenerateGb(this PlayerComponentBundle player, BankActionType type, IBankService bankService)
    {
        return new GbPacket
        {
            Type = type,
            GoldBank = player.BankGold / 1000,
            Gold = (int)player.Gold,
            BankRank = player.GetBankRank(bankService),
            BankTax = (int)player.GetBankWithdrawalFee(bankService)
        };
    }

    public static SMemoi2Packet GenerateBankBalance(this PlayerComponentBundle player) => new()
    {
        Type = SMemoType.BankNormal,
        Message = Game18NConstString.BalanceBank,
        ArgumentType = 3,
        Game18NArguments = { player.BankGold / 1000, player.Gold }
    };

    public static bool HasBankVip(this PlayerComponentBundle player) =>
        player.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.CuarryBankVip);

    public static BankRankType GetBankRank(this PlayerComponentBundle player, IBankService bankService) =>
        player.HasBankVip() ? BankRankType.Vip : bankService.GetRank(ReputationLevels.FromReputation(player.Reputation));

    public static long GetBankWithdrawalFee(this PlayerComponentBundle player, IBankService bankService) =>
        player.HasBankVip() ? 0 : bankService.GetWithdrawalFee(ReputationLevels.FromReputation(player.Reputation));

    public static LevPacket GenerateLev(this PlayerComponentBundle player,
        IExperienceService experienceService,
        IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService, ISkillPointService skillPointService)
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
            SkillCp = player.RemainingSkillPoints(skillPointService),
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
            ReputIcon = (int)player.ReputIcon,
            Dignity = player.Dignity,
            DignityIcon = (int)player.DignityIcon
        };
    }

    // The right-click info card for a player: class, level, worn gear, faction and PvP stats.
    // Family, talent and Act4 fields stay 0 until those systems exist.
    public static TcInfoPacket GenerateReqInfo(this PlayerComponentBundle player)
    {
        var inv = player.InventoryService;
        var weapon = (inv.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear)
            ?.ItemInstance) as WearableInstance;
        var secondary = (inv.LoadBySlotAndType((short)EquipmentType.SecondaryWeapon, NoscorePocketType.Wear)
            ?.ItemInstance) as WearableInstance;
        var armor = (inv.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear)
            ?.ItemInstance) as WearableInstance;
        var fairy = (inv.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)
            ?.ItemInstance) as WearableInstance;

        return new TcInfoPacket
        {
            Level = player.Level,
            Name = player.Name,
            Element = (ElementType)(fairy?.Item?.Element ?? 0),
            ElementRate = (fairy?.ElementRate ?? 0) + (fairy?.Item?.ElementRate ?? 0),
            Class = player.Class,
            Gender = player.Gender,
            ReputationIco = (ExtendedReputationType)player.ReputIcon,
            DignityIco = (CharacterDignity)player.DignityIcon,
            HaveWeapon = weapon != null ? 1 : 0,
            WeaponRare = weapon?.Rare ?? 0,
            WeaponUpgrade = weapon?.Upgrade ?? 0,
            HaveSecondary = secondary != null ? 1 : 0,
            SecondaryRare = secondary?.Rare ?? 0,
            SecondaryUpgrade = secondary?.Upgrade ?? 0,
            HaveArmor = armor != null ? 1 : 0,
            ArmorRare = armor?.Rare ?? 0,
            ArmorUpgrade = armor?.Upgrade ?? 0,
            Act4Kill = 0,
            Act4Dead = 0,
            Reputation = player.Reputation,
            Morph = 0,
            TalentWin = 0,
            TalentLose = 0,
            TalentSurrender = 0,
            MasterPoints = 0,
            Compliments = player.Compliment,
            Act4Points = 0,
            IsPvpPrimary = false,
            IsPvpSecondary = false,
            IsPvpArmor = false,
            HeroLevel = player.HeroLevel,
            Biography = string.Empty,
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

    public static CInfoPacket GenerateCInfo(this PlayerComponentBundle player, IGameLanguageLocalizer localizer)
    {
        return new CInfoPacket
        {
            Name = player.Authority == AuthorityType.Moderator
                ? $"[{localizer[LanguageKey.SUPPORT, player.AccountLanguage]}]" + player.Name : player.Name,
            Unknown1 = null,
            GroupId = -1,
            FamilyId = (int)(player.Family?.FamilyId ?? -1),
            FamilyTitle = player.Family == null ? null : (short)-1,
            FamilyName = player.GenerateFamilyTag(localizer, player.AccountLanguage),
            CharacterId = player.VisualId,
            Authority = player.Authority,
            Gender = player.Gender,
            HairStyle = player.HairStyle,
            HairColor = player.HairColor,
            Class = player.Class,
            Icon = GetIcon(player),
            Compliment = (short)(player.Authority == AuthorityType.Moderator ? 500 : player.Compliment),
            Morph = 0,
            Invisible = false,
            FamilyLevel = 0,
            MorphUpgrade = 0,
            ArenaWinner = false
        };
    }

    // The icon slot carries one of the two ladders: the reputation icon while dignity is
    // untouched, and the negated dignity icon once it drops into a penalty band.
    private static byte GetIcon(PlayerComponentBundle player)
    {
        var dignity = DignityLevels.FromDignity(player.Dignity);
        return dignity == DignityType.Default
            ? (byte)player.ReputIcon
            : (byte)-(int)dignity;
    }

    // A worn wig is drawn in its own colour instead of the hair's.
    public static HairColorType ShownHairColor(this PlayerComponentBundle player)
    {
        var hat = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Hat, NoscorePocketType.Wear);
        return hat?.ItemInstance.Item.IsColored == true ? (HairColorType)hat.ItemInstance.Design : player.HairColor;
    }

    public static EqPacket GenerateEq(this PlayerComponentBundle player)
    {
        return new EqPacket
        {
            VisualId = player.VisualId,
            Visibility = (byte)(player.Authority < AuthorityType.GameMaster ? 0 : 2),
            Gender = player.Gender,
            HairStyle = player.HairStyle,
            Haircolor = player.ShownHairColor(),
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
                VNum = eq.ItemInstance.LookVNum(),
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

    // The character window shows what combat uses, worn gear and active effects included. Combat
    // keeps a debuffed attack or defence below zero, but the window stops each at 0.
    public static ScPacket GenerateSc(this PlayerComponentBundle player, IBattleStatsProvider battleStatsProvider)
    {
        var stats = battleStatsProvider.GetStats(player);
        var (mainType, secondaryType) = player.Class switch
        {
            CharacterClassType.Archer => (AttackType.Ranged, AttackType.Melee),
            CharacterClassType.Mage => (AttackType.Magical, AttackType.Ranged),
            _ => (AttackType.Melee, AttackType.Ranged),
        };
        return new ScPacket
        {
            Type = (byte)mainType,
            MainWeaponUpgrade = (byte)stats.MeleeUpgrade,
            MinHit = Math.Max(0, stats.MinHit),
            MaxHit = Math.Max(0, stats.MaxHit),
            HitRate = stats.HitRate,
            CriticalHitRate = (short)stats.CriticalChance,
            CriticalHitMultiplier = stats.CriticalRate,
            Type2 = (byte)secondaryType,
            SecondaryWeaponUpgrade = (short)stats.RangedUpgrade,
            SecondaryMinHit = (short)Math.Max(0, stats.MinDistance),
            SecondaryMaxHit = (short)Math.Max(0, stats.MaxDistance),
            SecondaryHitRate = (short)stats.DistanceRate,
            SecondaryCriticalHitRate = (short)stats.DistanceCriticalChance,
            SecondaryCriticalHitMultiplier = (short)stats.DistanceCriticalRate,
            ArmorUpgrade = (byte)stats.DefenceUpgrade,
            Defence = (short)Math.Max(0, stats.Defence),
            DefenceRate = (short)stats.DefenceDodge,
            DistanceDefence = (short)Math.Max(0, stats.DistanceDefence),
            DistanceDefenceRate = (short)stats.DistanceDefenceDodge,
            MagicalDefence = (short)Math.Max(0, stats.MagicDefence),
            FireResistance = (short)stats.FireResistance,
            WaterResistance = (short)stats.WaterResistance,
            LightResistance = (short)stats.LightResistance,
            DarkResistance = (short)stats.DarkResistance,
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
                    MapX = mp?.MapX ?? 0,
                    MapY = mp?.MapY ?? 0,
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
                    await session.SendPacketAsync(mail.GeneratePost(0));
                }
                else
                {
                    await session.SendPacketAsync(mail.GeneratePost(1));
                }
            }
            else
            {
                if (mail.ItemInstance != null)
                {
                    await session.SendPacketAsync(mail.GeneratePost(3));
                }
                else
                {
                    await session.SendPacketAsync(mail.GeneratePost(2));
                }
            }
        }
    }

    public static async Task ChangeClassAsync(this ClientSession session, CharacterClassType classType,
        IOptions<WorldConfiguration> worldConfiguration,
        IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISkillPointService skillPointService,
        IItemGenerationService itemProvider, Services.SkillService.ISkillService skillService)
    {
        var character = session.Character;
        var inventoryService = character.InventoryService;
        var characterId = character.CharacterId;
        var mapInstance = character.MapInstance;
        var group = character.Group;

        if (inventoryService.Any(s => s.Value.Type == NoscorePocketType.Wear))
        {
            await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.RemoveEquipment));
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

        // The old class's skills are no longer usable and the new one's are not there yet:
        // without this you change job and keep the previous bar, full of icons the client
        // refuses to cast because they do not belong to the class.
        character.Skills.Clear();

        // Emptying the list is not enough - the rows behind it survive, and the next login loads
        // them straight back on top of the new class's. LearnClassSkillsAsync then grants what
        // the job level allows, and the change has just put that back to 1, so it starts from
        // the first skill.
        await skillService.ForgetUnlearnableSkillsAsync(character).ConfigureAwait(false);
        await skillService.LearnClassSkillsAsync(character).ConfigureAwait(false);

        var itemsToAdd = worldConfiguration.Value.BasicEquipments.TryGetValue(classType.ToString(), out var byOrigin)
            && byOrigin.TryGetValue(StarterOrigin.CreateAndUpgrade, out var pack)
            ? pack
            : new List<BasicEquipment>();

        foreach (var itemToAdd in itemsToAdd)
        {
            var inv = inventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemProvider.Create(itemToAdd.VNum, itemToAdd.Amount), characterId),
                itemToAdd.NoscorePocketType);
            if (inv != null)
            {
                await session.SendPacketsAsync(
                    inv.Select(invItem => invItem.GeneratePocketChange(invItem.Type.ToPocketType(), invItem.Slot)));
            }
        }

        character = session.Character;
        var titPacket = character.GenerateTit();
        var statPacket = character.GenerateStat();
        var eqPacket = character.GenerateEq();
        var effPacket8 = character.GenerateEff(8);
        var condPacket = character.GenerateCond();
        var levPacket = character.GenerateLev(experienceService, jobExperienceService, heroExperienceService, skillPointService);
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
        var channelId = session.ConnectedChannel.Id;
        var inPacket = character.GenerateIn("");
        var pidxPacket = group.GeneratePidx(character);
        var effPacket6 = character.GenerateEff(6);
        var effPacket198 = character.GenerateEff(198);

        await mapInstance.SendPacketAsync(inPacket, new EveryoneBut(channelId));
        await mapInstance.SendPacketAsync(pidxPacket);
        await mapInstance.SendPacketAsync(effPacket6);
        await mapInstance.SendPacketAsync(effPacket198);
    }
}
