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

using Microsoft.Extensions.Options;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data;
using WebApiCharacter = NosCore.Data.WebApi.Character;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.ServerPackets.Relations;

namespace NosCore.GameObject.Ecs.Systems;

public interface ICharacterPacketSystem
{
    IEnumerable<IPacket> GenerateInv(PlayerContext player, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer);
    GoldPacket GenerateGold(PlayerContext player);
    SkillPacket GenerateSki(PlayerContext player);
    IEnumerable<QSlotPacket> GenerateQuicklist(PlayerContext player);
    LevPacket GenerateLev(PlayerContext player, IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService);
    RsfiPacket GenerateRsfi(PlayerContext player);
    MlobjlstPacket GenerateMlobjlst(PlayerContext player);
    ServerExcListPacket GenerateServerExcListPacket(PlayerContext player, long? gold, long? bankGold, List<ServerExcListSubPacket?>? subPackets);
    TitleInfoPacket GenerateTitInfo(PlayerContext player);
    TitlePacket GenerateTitle(PlayerContext player);
    ExtsPacket GenerateExts(PlayerContext player, IOptions<WorldConfiguration> conf);
    ClPacket GenerateInvisible(PlayerContext player);
    ServerGetPacket GenerateGet(PlayerContext player, long itemId);
    QstlistPacket GenerateQuestPacket(PlayerContext player);
    IconPacket GenerateIcon(PlayerContext player, byte iconType, short iconParameter);
    OutPacket GenerateOut(PlayerContext player);
    InPacket GenerateIn(PlayerContext player, string prefix);
    InEquipmentSubPacket GetEquipmentSubPacket(PlayerContext player);
    UpgradeRareSubPacket GetWeaponUpgradeRareSubPacket(PlayerContext player);
    UpgradeRareSubPacket GetArmorUpgradeRareSubPacket(PlayerContext player);
    FdPacket GenerateFd(PlayerContext player);
    AtPacket GenerateAt(PlayerContext player);
    TitPacket GenerateTit(PlayerContext player);
    TalkPacket GenerateTalk(PlayerContext player, string message);
    EquipPacket? GenerateEquipment(PlayerContext player);
    EqPacket GenerateEq(PlayerContext player);
    CInfoPacket GenerateCInfo(PlayerContext player);
    Task<BlinitPacket> GenerateBlinitAsync(PlayerContext player, IBlacklistHub blacklistHttpClient);
    Task<FinitPacket> GenerateFinitAsync(PlayerContext player, IFriendHub friendHttpClient, IChannelHub channelHttpClient, IPubSubHub pubSubHub);
    Task SendFinfoAsync(PlayerContext player, IFriendHub friendHttpClient, IPubSubHub pubSubHub, ISerializer packetSerializer, bool isConnected);
}

public class CharacterPacketSystem(
    IReputationService reputationService,
    IDignityService dignityService,
    IGameLanguageLocalizer gameLanguageLocalizer) : ICharacterPacketSystem
{
    [Obsolete("GenerateStartupInventory should be used only on startup")]
    public IEnumerable<IPacket> GenerateInv(PlayerContext player, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer)
    {
        var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket?>() };
        var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket?>() };
        var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket?>() };
        var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket?>() };
        var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket?>() };
        var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket?>() };

        if (player.InventoryService == null)
        {
            return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7 };
        }

        foreach (var inv in player.InventoryService.Select(s => s.Value))
        {
            switch (inv.Type)
            {
                case NoscorePocketType.Equipment:
                    if (inv.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
                    {
                        if (inv.ItemInstance is SpecialistInstance specialistInstance)
                        {
                            inv7.IvnSubPackets.Add(new IvnSubPacket
                            {
                                Slot = inv.Slot,
                                VNum = inv.ItemInstance.ItemVNum,
                                RareAmount = specialistInstance.Rare,
                                UpgradeDesign = specialistInstance.Upgrade,
                                SecondUpgrade = specialistInstance.SpStoneUpgrade
                            });
                        }
                    }
                    else
                    {
                        if (inv.ItemInstance is WearableInstance wearableInstance)
                        {
                            inv0.IvnSubPackets.Add(new IvnSubPacket
                            {
                                Slot = inv.Slot,
                                VNum = inv.ItemInstance.ItemVNum,
                                RareAmount = wearableInstance.Rare,
                                UpgradeDesign = inv.ItemInstance.Item.IsColored ? wearableInstance.Design
                                    : wearableInstance.Upgrade
                            });
                        }
                    }
                    break;

                case NoscorePocketType.Main:
                    inv1.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Etc:
                    inv2.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Miniland:
                    inv3.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Specialist:
                    if (inv.ItemInstance is SpecialistInstance specialist)
                    {
                        inv6.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot,
                            VNum = inv.ItemInstance.ItemVNum,
                            RareAmount = specialist.Rare,
                            UpgradeDesign = specialist.Upgrade,
                            SecondUpgrade = specialist.SpStoneUpgrade
                        });
                    }
                    break;

                case NoscorePocketType.Costume:
                    if (inv.ItemInstance is WearableInstance costumeInstance)
                    {
                        inv7.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot,
                            VNum = inv.ItemInstance.ItemVNum,
                            RareAmount = costumeInstance.Rare,
                            UpgradeDesign = costumeInstance.Upgrade
                        });
                    }
                    break;

                case NoscorePocketType.Wear:
                    break;
                default:
                    logger.Information(logLanguageLocalizer[LogLanguageKey.POCKETTYPE_UNKNOWN]);
                    break;
            }
        }

        return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7 };
    }

    public GoldPacket GenerateGold(PlayerContext player)
    {
        return new GoldPacket { Gold = player.Gold };
    }

    public SkillPacket GenerateSki(PlayerContext player)
    {
        var characterSkills = player.Skills.Values.OrderBy(s => s.Skill?.CastId).ToList();
        return new SkillPacket
        {
            MainSkill = !player.UseSp ? 201 + 20 * (byte)player.Class : characterSkills.ElementAt(0).SkillVNum,
            SecondarySkill = !player.UseSp ? 200 + 20 * (byte)player.Class : characterSkills.ElementAt(0).SkillVNum,
            Skills = characterSkills.Select(x => new SubSkillPacket { VNum = x.SkillVNum }).ToList()
        };
    }

    public IEnumerable<QSlotPacket> GenerateQuicklist(PlayerContext player)
    {
        var pktQs = new QSlotPacket[2];
        for (var i = 0; i < pktQs.Length; i++)
        {
            var subpacket = new List<QsetClientSubPacket?>();
            for (var j = 0; j < 30; j++)
            {
                var qi = player.QuicklistEntries.FirstOrDefault(n =>
                    (n.QuickListIndex == i) && (n.Slot == j) && (n.Morph == (player.UseSp ? player.Morph : 0)));

                subpacket.Add(new QsetClientSubPacket
                {
                    OriginQuickList = qi?.Type ?? 7,
                    OriginQuickListSlot = qi?.IconType ?? -1,
                    Data = qi?.IconVNum ?? -1
                });
            }

            pktQs[i] = new QSlotPacket { Slot = i, Data = subpacket };
        }

        return pktQs;
    }

    public LevPacket GenerateLev(PlayerContext player, IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
    {
        var level = player.Level;
        var jobLevel = player.JobLevel;
        var heroLevel = player.HeroLevel;
        return new LevPacket
        {
            Level = level,
            LevelXp = player.LevelXp,
            JobLevel = jobLevel,
            JobLevelXp = player.JobLevelXp,
            XpLoad = experienceService.GetExperience(level),
            JobXpLoad = jobExperienceService.GetJobExperience(player.Class, jobLevel),
            Reputation = player.Reput,
            SkillCp = 0,
            HeroXp = player.HeroXp,
            HeroLevel = heroLevel,
            HeroXpLoad = heroLevel == 0 ? 0 : heroExperienceService.GetHeroExperience(heroLevel)
        };
    }

    public RsfiPacket GenerateRsfi(PlayerContext player)
    {
        return new RsfiPacket
        {
            Act = 1,
            ActPart = 1,
            Unknown1 = 0,
            Unknown2 = 9,
            Ts = 0,
            TsMax = 9
        };
    }

    public MlobjlstPacket GenerateMlobjlst(PlayerContext player)
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

        return new MlobjlstPacket { MlobjlstSubPacket = mlobj };
    }

    public ServerExcListPacket GenerateServerExcListPacket(PlayerContext player, long? gold, long? bankGold, List<ServerExcListSubPacket?>? subPackets)
    {
        return new ServerExcListPacket
        {
            VisualType = player.VisualType,
            VisualId = player.VisualId,
            Gold = gold,
            SubPackets = subPackets,
            BankGold = bankGold
        };
    }

    public TitleInfoPacket GenerateTitInfo(PlayerContext player)
    {
        var visibleTitle = player.Titles.FirstOrDefault(s => s.Visible)?.TitleType;
        var effectiveTitle = player.Titles.FirstOrDefault(s => s.Active)?.TitleType;
        return new TitleInfoPacket
        {
            VisualId = player.VisualId,
            EffectiveTitle = effectiveTitle ?? 0,
            VisualType = player.VisualType,
            VisibleTitle = visibleTitle ?? 0,
        };
    }

    public TitlePacket GenerateTitle(PlayerContext player)
    {
        var data = player.Titles.Select(s => new TitleSubPacket
        {
            TitleId = (short)(s.TitleType - 9300),
            TitleStatus = (byte)((s.Visible ? 2 : 0) + (s.Active ? 4 : 0) + 1)
        }).ToList() as List<TitleSubPacket?>;
        return new TitlePacket { Data = data.Any() ? data : null };
    }

    public ExtsPacket GenerateExts(PlayerContext player, IOptions<WorldConfiguration> conf)
    {
        return new ExtsPacket
        {
            EquipmentExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Equipment] + conf.Value.BackpackSize),
            MainExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Main] + conf.Value.BackpackSize),
            EtcExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Etc] + conf.Value.BackpackSize)
        };
    }

    public ClPacket GenerateInvisible(PlayerContext player)
    {
        return new ClPacket
        {
            VisualId = player.VisualId,
            Camouflage = player.Camouflage,
            Invisible = player.Invisible
        };
    }

    public ServerGetPacket GenerateGet(PlayerContext player, long itemId)
    {
        return new ServerGetPacket
        {
            VisualType = player.VisualType,
            VisualId = player.VisualId,
            ItemId = itemId
        };
    }

    public QstlistPacket GenerateQuestPacket(PlayerContext player)
    {
        return new QstlistPacket(player.Quests.Values
            .Where(s => s.CompletedOn == null).Select(quest => quest.GenerateQuestSubPacket(true)).ToList());
    }

    public IconPacket GenerateIcon(PlayerContext player, byte iconType, short iconParameter)
    {
        return new IconPacket
        {
            VisualType = player.VisualType,
            VisualId = player.VisualId,
            IconParameter = iconParameter,
            IconType = iconType
        };
    }

    public OutPacket GenerateOut(PlayerContext player)
    {
        return new OutPacket
        {
            VisualType = player.VisualType,
            VisualId = player.VisualId
        };
    }

    public InPacket GenerateIn(PlayerContext player, string prefix)
    {
        var reput = player.Reput;
        var dignity = player.Dignity;
        var dignityIcon = dignityService.GetLevelFromDignity(dignity);
        var reputIcon = reputationService.GetLevelFromReputation(reput);
        return new InPacket
        {
            VisualType = player.VisualType,
            Name = prefix + player.Name,
            VNum = player.VNum == 0 ? null : player.VNum.ToString(),
            VisualId = player.VisualId,
            PositionX = player.PositionX,
            PositionY = player.PositionY,
            Direction = player.Direction,
            InCharacterSubPacket = new InCharacterSubPacket
            {
                Authority = player.Authority >= AuthorityType.Administrator ? AuthorityType.Administrator : AuthorityType.User,
                Gender = player.Gender,
                HairStyle = player.HairStyle,
                HairColor = player.HairColor,
                Class = player.Class,
                Equipment = GetEquipmentSubPacket(player),
                InAliveSubPacket = new InAliveSubPacket
                {
                    Hp = (int)(player.Hp / (float)player.MaxHp * 100),
                    Mp = (int)(player.Mp / (float)player.MaxMp * 100)
                },
                IsSitting = player.IsSitting,
                GroupId = player.Group!.GroupId,
                Fairy = 0,
                FairyElement = 0,
                Unknown = 0,
                Morph = 0,
                Unknown2 = 0,
                Unknown3 = 0,
                WeaponUpgradeRareSubPacket = GetWeaponUpgradeRareSubPacket(player),
                ArmorUpgradeRareSubPacket = GetArmorUpgradeRareSubPacket(player),
                FamilySubPacket = new FamilySubPacket(),
                FamilyName = null,
                ReputIco = (byte)(dignityIcon == DignityType.Default ? (byte)reputIcon
                    : -(byte)dignityIcon),
                Invisible = false,
                MorphUpgrade = 0,
                Faction = 0,
                MorphUpgrade2 = 0,
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

    public InEquipmentSubPacket GetEquipmentSubPacket(PlayerContext player) => new()
    {
        Armor = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        CostumeHat = player.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeHat, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        CostumeSuit = player.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeSuit, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        Fairy = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        Hat = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Hat, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        MainWeapon = player.InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        Mask = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Mask, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        SecondaryWeapon = player.InventoryService.LoadBySlotAndType((short)EquipmentType.SecondaryWeapon, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        WeaponSkin = player.InventoryService.LoadBySlotAndType((short)EquipmentType.WeaponSkin, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
        WingSkin = player.InventoryService.LoadBySlotAndType((short)EquipmentType.WingSkin, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum
    };

    public UpgradeRareSubPacket GetWeaponUpgradeRareSubPacket(PlayerContext player)
    {
        var weapon = player.InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear);
        return new UpgradeRareSubPacket
        {
            Upgrade = weapon?.ItemInstance?.Upgrade ?? 0,
            Rare = (sbyte)(weapon?.ItemInstance?.Rare ?? 0)
        };
    }

    public UpgradeRareSubPacket GetArmorUpgradeRareSubPacket(PlayerContext player)
    {
        var armor = player.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear);
        return new UpgradeRareSubPacket
        {
            Upgrade = armor?.ItemInstance?.Upgrade ?? 0,
            Rare = (sbyte)(armor?.ItemInstance?.Rare ?? 0)
        };
    }

    public FdPacket GenerateFd(PlayerContext player)
    {
        var reput = player.Reput;
        var dignity = player.Dignity;
        return new FdPacket
        {
            Reput = reput,
            Dignity = dignity,
            ReputIcon = (int)reputationService.GetLevelFromReputation(reput),
            DignityIcon = (int)dignityService.GetLevelFromDignity(dignity)
        };
    }

    public AtPacket GenerateAt(PlayerContext player)
    {
        return new AtPacket
        {
            CharacterId = player.VisualId,
            MapId = player.MapInstance.Map.MapId,
            PositionX = player.PositionX,
            PositionY = player.PositionY,
            Direction = player.Direction,
            Unknown1 = 0,
            Music = player.MapInstance.Map.Music,
            Unknown2 = 0,
            Unknown3 = -1
        };
    }

    public TitPacket GenerateTit(PlayerContext player)
    {
        return new TitPacket
        {
            ClassType = (Game18NConstString)Enum.Parse(typeof(Game18NConstString), player.Class.ToString()),
            Name = player.Name
        };
    }

    public TalkPacket GenerateTalk(PlayerContext player, string message)
    {
        return new TalkPacket
        {
            CharacterId = player.VisualId,
            Message = message
        };
    }

    public EquipPacket? GenerateEquipment(PlayerContext player)
    {
        EquipmentSubPacket? GenerateEquipmentSubPacket(EquipmentType eqType)
        {
            var eq = player.InventoryService.LoadBySlotAndType((short)eqType, NoscorePocketType.Wear);
            if (eq == null)
            {
                return null;
            }

            return new EquipmentSubPacket
            {
                EquipmentType = eqType,
                VNum = eq.ItemInstance.ItemVNum,
                Rare = eq.ItemInstance.Rare,
                Upgrade = (eq.ItemInstance.Item.IsColored ? eq.ItemInstance?.Design : eq.ItemInstance.Upgrade) ?? 0,
                Unknown = 0
            };
        }

        return new EquipPacket
        {
            WeaponUpgradeRareSubPacket = GetWeaponUpgradeRareSubPacket(player),
            ArmorUpgradeRareSubPacket = GetArmorUpgradeRareSubPacket(player),
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

    public EqPacket GenerateEq(PlayerContext player)
    {
        return new EqPacket
        {
            VisualId = player.VisualId,
            Visibility = (byte)(player.Authority < AuthorityType.GameMaster ? 0 : 2),
            Gender = player.Gender,
            HairStyle = player.HairStyle,
            Haircolor = player.HairColor,
            ClassType = player.Class,
            EqSubPacket = GetEquipmentSubPacket(player),
            WeaponUpgradeRarePacket = GetWeaponUpgradeRareSubPacket(player),
            ArmorUpgradeRarePacket = GetArmorUpgradeRareSubPacket(player),
            Size = player.Size
        };
    }

    public CInfoPacket GenerateCInfo(PlayerContext player)
    {
        var name = player.Name;
        var reput = player.Reput;
        var dignity = player.Dignity;
        var dignityIcon = dignityService.GetLevelFromDignity(dignity);
        var reputIcon = reputationService.GetLevelFromReputation(reput);
        return new CInfoPacket
        {
            Name = player.Authority == AuthorityType.Moderator
                ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, player.AccountLanguage]}]" + name : name,
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
            Icon = (byte)(dignityIcon == DignityType.Default ? (byte)reputIcon : -(byte)dignityIcon),
            Compliment = (short)(player.Authority == AuthorityType.Moderator ? 500 : player.Compliment),
            Morph = 0,
            Invisible = false,
            FamilyLevel = 0,
            MorphUpgrade = 0,
            ArenaWinner = false
        };
    }

    public async Task<BlinitPacket> GenerateBlinitAsync(PlayerContext player, IBlacklistHub blacklistHttpClient)
    {
        var subpackets = new List<BlinitSubPacket?>();
        var blackList = await blacklistHttpClient.GetBlacklistedAsync(player.VisualId).ConfigureAwait(false);
        foreach (var relation in blackList)
        {
            if (relation.CharacterId == player.VisualId)
            {
                continue;
            }

            subpackets.Add(new BlinitSubPacket
            {
                RelatedCharacterId = relation.CharacterId,
                CharacterName = relation.CharacterName
            });
        }

        return new BlinitPacket { SubPackets = subpackets };
    }

    public async Task<FinitPacket> GenerateFinitAsync(PlayerContext player, IFriendHub friendHttpClient, IChannelHub channelHttpClient, IPubSubHub pubSubHub)
    {
        var servers = (await channelHttpClient.GetCommunicationChannels().ConfigureAwait(false))
            ?.Where(c => c.Type == ServerType.WorldServer).ToList();
        var accounts = await pubSubHub.GetSubscribersAsync();

        var subpackets = new List<FinitSubPacket?>();
        var friendlist = await friendHttpClient.GetFriendsAsync(player.VisualId).ConfigureAwait(false);
        foreach (var relation in friendlist)
        {
            var account = accounts.Find(s =>
                (s.ConnectedCharacter != null) && (s.ConnectedCharacter.Id == relation.CharacterId));
            subpackets.Add(new FinitSubPacket
            {
                CharacterId = relation.CharacterId,
                RelationType = relation.RelationType,
                IsOnline = account != null,
                CharacterName = relation.CharacterName
            });
        }

        return new FinitPacket { SubPackets = subpackets };
    }

    public async Task SendFinfoAsync(PlayerContext player, IFriendHub friendHttpClient, IPubSubHub pubSubHub, ISerializer packetSerializer, bool isConnected)
    {
        var friendlist = await friendHttpClient.GetFriendsAsync(player.VisualId).ConfigureAwait(false);
        var name = player.Name;
        await Task.WhenAll(friendlist.Select(friend =>
            pubSubHub.SendMessageAsync(new PostedPacket
            {
                Packet = packetSerializer.Serialize(new[]
                {
                   new FinfoPacket
                   {
                       FriendList = new List<FinfoSubPackets?>
                       {
                           new()
                           {
                               CharacterId = player.VisualId,
                               IsConnected = isConnected
                           }
                       }
                   }
                }),
                ReceiverType = ReceiverType.OnlySomeone,
                SenderCharacter = new WebApiCharacter { Id = player.VisualId, Name = name },
                ReceiverCharacter = new WebApiCharacter
                {
                    Id = friend.CharacterId,
                    Name = friend.CharacterName!
                }
            }))).ConfigureAwait(false);
    }
}
