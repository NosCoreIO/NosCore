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
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostedPacket = NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class CharacterEntityExtension
    {
        [Obsolete(
           "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
        public static IEnumerable<IPacket> GenerateInv(this ICharacterEntity characterEntity, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer)
        {
            var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket?>() };

            if (characterEntity.InventoryService == null)
            {
                return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7 };
            }

            foreach (var inv in characterEntity.InventoryService.Select(s => s.Value))
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
                        logger.Information(
                            logLanguageLocalizer[LogLanguageKey.POCKETTYPE_UNKNOWN]);
                        break;
                }
            }

            return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7 };
        }

        public static GoldPacket GenerateGold(this ICharacterEntity characterEntity)
        {
            return new GoldPacket { Gold = characterEntity.Gold };
        }

        public static SkillPacket GenerateSki(this ICharacterEntity characterEntity)
        {
            List<CharacterSkill> characterSkills = characterEntity.Skills.Values.OrderBy(s => s.Skill?.CastId).ToList();
            var packet = new SkillPacket
            {
                MainSkill = !characterEntity.UseSp ? 201 + 20 * (byte)characterEntity.Class : characterSkills.ElementAt(0).SkillVNum,
                SecondarySkill = !characterEntity.UseSp ? 200 + 20 * (byte)characterEntity.Class : characterSkills.ElementAt(0).SkillVNum,
                Skills = characterSkills.Select(x => new SubSkillPacket()
                {
                    VNum = x.SkillVNum
                }).ToList()
            };
            return packet;
        }

        public static IEnumerable<QSlotPacket> GenerateQuicklist(this ICharacterEntity characterEntity)
        {
            var pktQs = new QSlotPacket[2];
            for (var i = 0; i < pktQs.Length; i++)
            {
                var subpacket = new List<QsetClientSubPacket?>();
                for (var j = 0; j < 30; j++)
                {
                    var qi = characterEntity.QuicklistEntries.FirstOrDefault(n =>
                        (n.QuickListIndex == i) && (n.Slot == j) && (n.Morph == (characterEntity.UseSp ? characterEntity.Morph : 0)));

                    subpacket.Add(new QsetClientSubPacket
                    {
                        OriginQuickList = qi?.Type ?? 7,
                        OriginQuickListSlot = qi?.IconType ?? -1,
                        Data = qi?.IconVNum ?? -1
                    });
                }

                pktQs[i] = new QSlotPacket
                {
                    Slot = i,
                    Data = subpacket
                };
            }

            return pktQs;
        }

        public static LevPacket GenerateLev(this ICharacterEntity characterEntity, IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
        {
            return new LevPacket
            {
                Level = characterEntity.Level,
                LevelXp = characterEntity.LevelXp,
                JobLevel = characterEntity.JobLevel,
                JobLevelXp = characterEntity.JobLevelXp,
                XpLoad = experienceService.GetExperience(characterEntity.Level),
                JobXpLoad = jobExperienceService.GetJobExperience(characterEntity.Class, characterEntity.JobLevel),
                Reputation = characterEntity.Reput,
                SkillCp = 0,
                HeroXp = characterEntity.HeroXp,
                HeroLevel = characterEntity.HeroLevel,
                HeroXpLoad = characterEntity.HeroLevel == 0 ? 0 : heroExperienceService.GetHeroExperience(characterEntity.HeroLevel)
            };
        }

        public static void LoadExpensions(this ICharacterEntity characterEntity)
        {
            var backpack = characterEntity.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BackPack);
            var backpackticket = characterEntity.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.InventoryTicketUpgrade);
            var expension = (byte)((backpack ? 12 : 0) + (backpackticket ? 60 : 0));

            characterEntity.InventoryService.Expensions[NoscorePocketType.Main] += expension;
            characterEntity.InventoryService.Expensions[NoscorePocketType.Equipment] += expension;
            characterEntity.InventoryService.Expensions[NoscorePocketType.Etc] += expension;
        }

        public static RsfiPacket GenerateRsfi(this ICharacterEntity characterEntity)
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

        public static MlobjlstPacket GenerateMlobjlst(this ICharacterEntity characterEntity)
        {
            var mlobj = new List<MlobjlstSubPacket?>();
            foreach (var item in characterEntity.InventoryService.Where(s => s.Value.Type == NoscorePocketType.Miniland)
                .OrderBy(s => s.Value.Slot).Select(s => s.Value))
            {
                var used = characterEntity.MapInstance.MapDesignObjects.ContainsKey(item.Id);
                var mp = used ? characterEntity.MapInstance.MapDesignObjects[item.Id] : null;

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

        public static ServerExcListPacket GenerateServerExcListPacket(this ICharacterEntity aliveEntity, long? gold,
            long? bankGold, List<ServerExcListSubPacket?>? subPackets)
        {
            return new ServerExcListPacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Gold = gold,
                SubPackets = subPackets,
                BankGold = bankGold
            };
        }

        public static TitleInfoPacket GenerateTitInfo(this ICharacterEntity visualEntity)
        {
            var visibleTitle = visualEntity.Titles.FirstOrDefault(s => s.Visible)?.TitleType;
            var effectiveTitle = visualEntity.Titles.FirstOrDefault(s => s.Active)?.TitleType;
            return new TitleInfoPacket
            {
                VisualId = visualEntity.VisualId,
                EffectiveTitle = effectiveTitle ?? 0,
                VisualType = visualEntity.VisualType,
                VisibleTitle = visibleTitle ?? 0,
            };
        }

        public static TitlePacket GenerateTitle(this ICharacterEntity visualEntity)
        {
            var data = visualEntity.Titles.Select(s => new TitleSubPacket
            {
                TitleId = (short)(s.TitleType - 9300),
                TitleStatus = (byte)((s.Visible ? 2 : 0) + (s.Active ? 4 : 0) + 1)
            }).ToList() as List<TitleSubPacket?>;
            return new TitlePacket
            {
                Data = data.Any() ? data : null
            };
        }

        public static ExtsPacket GenerateExts(this ICharacterEntity visualEntity, IOptions<WorldConfiguration> conf)
        {
            return new ExtsPacket
            {
                EquipmentExtension = (byte)(visualEntity.InventoryService.Expensions[NoscorePocketType.Equipment] + conf.Value.BackpackSize),
                MainExtension = (byte)(visualEntity.InventoryService.Expensions[NoscorePocketType.Main] + conf.Value.BackpackSize),
                EtcExtension = (byte)(visualEntity.InventoryService.Expensions[NoscorePocketType.Etc] + conf.Value.BackpackSize)
            };
        }

        public static ClPacket GenerateInvisible(this ICharacterEntity visualEntity)
        {
            return new ClPacket
            {
                VisualId = visualEntity.VisualId,
                Camouflage = visualEntity.Camouflage,
                Invisible = visualEntity.Invisible
            };
        }

        public static async Task<BlinitPacket> GenerateBlinitAsync(this ICharacterEntity visualEntity,
            IBlacklistHub blacklistHttpClient)
        {
            var subpackets = new List<BlinitSubPacket?>();
            var blackList = await blacklistHttpClient.GetBlacklistedAsync(visualEntity.VisualId);
            foreach (var relation in blackList)
            {
                if (relation.CharacterId == visualEntity.VisualId)
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

        public static async Task<FinitPacket> GenerateFinitAsync(this ICharacterEntity visualEntity, IFriendHub friendHttpClient,
            IChannelHub channelHttpClient, IPubSubHub pubSubHub)
        {
            //same canal
            var servers = (await channelHttpClient.GetCommunicationChannels())
                ?.Where(c => c.Type == ServerType.WorldServer).ToList();
            var accounts = await pubSubHub.GetSubscribersAsync();

            var subpackets = new List<FinitSubPacket?>();
            var friendlist = await friendHttpClient.GetFriendsAsync(visualEntity.VisualId);
            //TODO add spouselist
            //var spouseList = _webApiAccess.Get<List<CharacterRelationDto>>(WebApiRoute.Spouse, friendServer.WebApi, visualEntity.VisualId) ?? new List<CharacterRelationDto>();
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

        public static async Task SendFinfoAsync(this ICharacterEntity visualEntity, IFriendHub friendHttpClient,
            IPubSubHub pubSubHub, ISerializer packetSerializer, bool isConnected)
        {
            var friendlist = await friendHttpClient.GetFriendsAsync(visualEntity.VisualId);
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
                                   CharacterId = visualEntity.VisualId,
                                   IsConnected = isConnected
                               }
                           }
                       }
                    }),
                    ReceiverType = ReceiverType.OnlySomeone,
                    SenderCharacter = new Data.WebApi.Character { Id = visualEntity.VisualId, Name = visualEntity.Name! },
                    ReceiverCharacter = new Data.WebApi.Character
                    {
                        Id = friend.CharacterId,
                        Name = friend.CharacterName!
                    }
                })));
        }

        public static ServerGetPacket GenerateGet(this ICharacterEntity visualEntity, long itemId)
        {
            return new ServerGetPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                ItemId = itemId
            };
        }

        public static QstlistPacket GenerateQuestPacket(this ICharacterEntity visualEntity)
        {
            return new QstlistPacket(visualEntity.Quests.Values
                .Where(s => s.CompletedOn == null).Select(quest => quest.GenerateQuestSubPacket(true)).ToList());
        }

        public static IconPacket GenerateIcon(this ICharacterEntity visualEntity, byte iconType, short iconParameter)
        {
            return new IconPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId,
                IconParameter = iconParameter,
                IconType = iconType
            };
        }

        public static OutPacket GenerateOut(this ICharacterEntity visualEntity)
        {
            return new OutPacket
            {
                VisualType = visualEntity.VisualType,
                VisualId = visualEntity.VisualId
            };
        }

        public static InPacket GenerateIn(this ICharacterEntity visualEntity, string prefix)
        {
            return new InPacket
            {
                VisualType = visualEntity.VisualType,
                Name = prefix + visualEntity.Name,
                VNum = visualEntity.VNum == 0 ? null : visualEntity.VNum.ToString(),
                VisualId = visualEntity.VisualId,
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                InCharacterSubPacket = new InCharacterSubPacket
                {
                    Authority = visualEntity.Authority >= AuthorityType.Administrator ? AuthorityType.Administrator : AuthorityType.User,
                    Gender = visualEntity.Gender,
                    HairStyle = visualEntity.HairStyle,
                    HairColor = visualEntity.HairColor,
                    Class = visualEntity.Class,
                    Equipment = visualEntity.GetEquipmentSubPacket(),
                    InAliveSubPacket = new InAliveSubPacket
                    {
                        Hp = (int)(visualEntity.Hp / (float)visualEntity.MaxHp * 100),
                        Mp = (int)(visualEntity.Mp / (float)visualEntity.MaxMp * 100)
                    },
                    IsSitting = visualEntity.IsSitting,
                    GroupId = visualEntity.Group!.GroupId,
                    Fairy = 0,
                    FairyElement = 0,
                    Unknown = 0,
                    Morph = 0,
                    Unknown2 = 0,
                    Unknown3 = 0,
                    WeaponUpgradeRareSubPacket = visualEntity.GetWeaponUpgradeRareSubPacket(),
                    ArmorUpgradeRareSubPacket = visualEntity.GetArmorUpgradeRareSubPacket(),
                    FamilySubPacket = new FamilySubPacket(),
                    FamilyName = null,
                    ReputIco = (byte)(visualEntity.DignityIcon == DignityType.Default ? (byte)visualEntity.ReputIcon
                        : -(byte)visualEntity.DignityIcon),//TODO replace type by a byte
                    Invisible = false,
                    MorphUpgrade = 0,
                    Faction = 0,
                    MorphUpgrade2 = 0,
                    Level = visualEntity.Level,
                    FamilyLevel = 0,
                    FamilyIcons = new List<bool> { false, false, false },
                    ArenaWinner = false,
                    Compliment = (short)(visualEntity.Authority >= AuthorityType.Moderator ? 500 : visualEntity.Compliment),
                    Size = visualEntity.Size,
                    HeroLevel = visualEntity.HeroLevel
                }
            };
        }

        public static InEquipmentSubPacket GetEquipmentSubPacket(this ICharacterEntity visualEntity) => new()
        {
            Armor = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            CostumeHat = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeHat, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            CostumeSuit = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeSuit, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            Fairy = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            Hat = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Hat, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            MainWeapon = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            Mask = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Mask, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            SecondaryWeapon = visualEntity.InventoryService
                .LoadBySlotAndType((short)EquipmentType.SecondaryWeapon, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            WeaponSkin = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.WeaponSkin, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            WingSkin = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.WingSkin, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum
        };

        public static UpgradeRareSubPacket GetWeaponUpgradeRareSubPacket(this ICharacterEntity visualEntity)
        {
            var weapon =
                visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear);
            return new UpgradeRareSubPacket
            {
                Upgrade = weapon?.ItemInstance?.Upgrade ?? 0,
                Rare = (sbyte)(weapon?.ItemInstance?.Rare ?? 0)
            };
        }

        public static UpgradeRareSubPacket GetArmorUpgradeRareSubPacket(this ICharacterEntity visualEntity)
        {
            var armor = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear);
            return new UpgradeRareSubPacket
            {
                Upgrade = armor?.ItemInstance?.Upgrade ?? 0,
                Rare = (sbyte)(armor?.ItemInstance?.Rare ?? 0)
            };

        }
        public static FdPacket GenerateFd(this ICharacterEntity visualEntity)
        {
            return new FdPacket
            {
                Reput = visualEntity.Reput,
                Dignity = visualEntity.Dignity,
                ReputIcon = (int)visualEntity.ReputIcon, //todo change packet type
                DignityIcon = (int)visualEntity.DignityIcon //todo change packet type
            };
        }

        public static AtPacket GenerateAt(this ICharacterEntity visualEntity)
        {
            return new AtPacket
            {
                CharacterId = visualEntity.VisualId,
                MapId = visualEntity.MapInstance.Map.MapId,
                PositionX = visualEntity.PositionX,
                PositionY = visualEntity.PositionY,
                Direction = visualEntity.Direction,
                Unknown1 = 0,
                Music = visualEntity.MapInstance.Map.Music,
                Unknown2 = 0,
                Unknown3 = -1
            };
        }

        public static TitPacket GenerateTit(this ICharacterEntity visualEntity)
        {
            return new TitPacket
            {
                ClassType = (Game18NConstString)Enum.Parse(typeof(Game18NConstString), visualEntity.Class.ToString()),
                Name = visualEntity.Name
            };
        }

        public static TalkPacket GenerateTalk(this ICharacterEntity visualEntity, string message)
        {
            return new TalkPacket
            {
                CharacterId = visualEntity.VisualId,
                Message = message
            };
        }


        public static EquipPacket? GenerateEquipment(this ICharacterEntity visualEntity)
        {
            EquipmentSubPacket? GenerateEquipmentSubPacket(EquipmentType eqType)
            {
                var eq = visualEntity.InventoryService.LoadBySlotAndType((short)eqType, NoscorePocketType.Wear);
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
                WeaponUpgradeRareSubPacket = visualEntity.GetWeaponUpgradeRareSubPacket(),
                ArmorUpgradeRareSubPacket = visualEntity.GetArmorUpgradeRareSubPacket(),
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

        public static EqPacket GenerateEq(this ICharacterEntity visualEntity)
        {
            return new EqPacket
            {
                VisualId = visualEntity.VisualId,
                Visibility = (byte)(visualEntity.Authority < AuthorityType.GameMaster ? 0 : 2),
                Gender = visualEntity.Gender,
                HairStyle = visualEntity.HairStyle,
                Haircolor = visualEntity.HairColor,
                ClassType = visualEntity.Class,
                EqSubPacket = visualEntity.GetEquipmentSubPacket(),
                WeaponUpgradeRarePacket = visualEntity.GetWeaponUpgradeRareSubPacket(),
                ArmorUpgradeRarePacket = visualEntity.GetArmorUpgradeRareSubPacket(),
                Size = visualEntity.Size
            };
        }


        public static CInfoPacket GenerateCInfo(this ICharacterEntity visualEntity)
        {
            return new CInfoPacket
            {
                Name = visualEntity.Authority == AuthorityType.Moderator
                    ? $"[{visualEntity.GetMessageFromKey(LanguageKey.SUPPORT)}]" + visualEntity.Name : visualEntity.Name,
                Unknown1 = null,
                GroupId = -1,
                FamilyId = -1,
                FamilyName = null,
                CharacterId = visualEntity.VisualId,
                Authority = visualEntity.Authority,
                Gender = visualEntity.Gender,
                HairStyle = visualEntity.HairStyle,
                HairColor = visualEntity.HairColor,
                Class = visualEntity.Class,
                Icon = (byte)(visualEntity.DignityIcon == DignityType.Default ? (byte)visualEntity.ReputIcon : -(byte)visualEntity.DignityIcon),
                Compliment = (short)(visualEntity.Authority == AuthorityType.Moderator ? 500 : visualEntity.Compliment),
                Morph = 0,
                Invisible = false,
                FamilyLevel = 0,
                MorphUpgrade = 0,
                ArenaWinner = false
            };
        }

        public static Task AddGoldAsync(this ICharacterEntity characterEntity)
        {
            return characterEntity.SendPacketAsync(characterEntity.GenerateGold());
        }

        public static Task AddGoldAsync(this ICharacterEntity characterEntity, long gold)
        {
            characterEntity.Gold += gold;
            return characterEntity.SendPacketAsync(characterEntity.GenerateGold());
        }

        public static Task RemoveGoldAsync(this ICharacterEntity characterEntity, long gold)
        {
            characterEntity.Gold -= gold;
            return characterEntity.SendPacketAsync(characterEntity.GenerateGold());
        }

        public static async Task SetGoldAsync(this ICharacterEntity characterEntity, long gold)
        {
            characterEntity.Gold = gold;
            await characterEntity.SendPacketAsync(characterEntity.GenerateGold());
            await characterEntity.SendPacketAsync(characterEntity.GenerateSay(
                characterEntity.GetMessageFromKey(LanguageKey.UPDATE_GOLD),
                SayColorType.Red));
        }

        public static async Task SetReputationAsync(this ICharacterEntity characterEntity, long reput)
        {
            characterEntity.Reput = reput;
            await characterEntity.SendPacketAsync(characterEntity.GenerateFd());
            await characterEntity.SendPacketAsync(characterEntity.GenerateSay(
                characterEntity.GetMessageFromKey(LanguageKey.REPUTATION_CHANGED),
                SayColorType.Red));
        }

        public static SpPacket GenerateSpPoint(this ICharacterEntity characterEntity, IOptions<WorldConfiguration> worldConfiguration)
        {
            return new SpPacket
            {
                AdditionalPoint = characterEntity.SpAdditionPoint,
                MaxAdditionalPoint = worldConfiguration.Value.MaxAdditionalSpPoints,
                SpPoint = characterEntity.SpPoint,
                MaxSpPoint = worldConfiguration.Value.MaxSpPoints
            };
        }

        public static StatPacket GenerateStat(this ICharacterEntity characterEntity)
        {
            return new StatPacket
            {
                Hp = characterEntity.Hp,
                HpMaximum = characterEntity.MaxHp,
                Mp = characterEntity.Mp,
                MpMaximum = characterEntity.MaxMp,
                Unknown = 0,
                Option = 0
            };
        }

        public static Task AddSpPointsAsync(this ICharacterEntity characterEntity, int spPointToAdd, IOptions<WorldConfiguration> worldConfiguration)
        {
            characterEntity.SpPoint = characterEntity.SpPoint + spPointToAdd > worldConfiguration.Value.MaxSpPoints
                ? worldConfiguration.Value.MaxSpPoints : characterEntity.SpPoint + spPointToAdd;
            return characterEntity.SendPacketAsync(characterEntity.GenerateSpPoint(worldConfiguration));
        }

        public static Task AddAdditionalSpPointsAsync(this ICharacterEntity characterEntity, int spPointToAdd, IOptions<WorldConfiguration> worldConfiguration)
        {
            characterEntity.SpAdditionPoint = characterEntity.SpAdditionPoint + spPointToAdd > worldConfiguration.Value.MaxAdditionalSpPoints
                ? worldConfiguration.Value.MaxAdditionalSpPoints : characterEntity.SpAdditionPoint + spPointToAdd;
            return characterEntity.SendPacketAsync(characterEntity.GenerateSpPoint(worldConfiguration));
        }

        public static async Task SetJobLevelAsync(this ICharacterEntity characterEntity, byte jobLevel,
            IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
        {
            characterEntity.JobLevel = (byte)((characterEntity.Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20 : jobLevel);
            characterEntity.JobLevelXp = 0;
            await characterEntity.SendPacketAsync(characterEntity.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
            await characterEntity.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.JobLevelIncreased
            });
        }

        public static async Task SetHeroLevelAsync(this ICharacterEntity characterEntity, byte level,
            IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
        {
            characterEntity.HeroLevel = level;
            characterEntity.HeroXp = 0;
            await characterEntity.SendPacketAsync(characterEntity.GenerateStat());
            await characterEntity.SendPacketAsync(characterEntity.GenerateStatInfo());
            await characterEntity.SendPacketAsync(characterEntity.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
            await characterEntity.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.HeroLevelIncreased
            });
        }

        public static void InitializeGroup(this ICharacterEntity characterEntity, ISessionGroupFactory sessionGroupFactory)
        {
            if (characterEntity.Group != null)
            {
                return;
            }

            characterEntity.Group = new Group(GroupType.Group, sessionGroupFactory);
            characterEntity.Group.JoinGroup(characterEntity);
        }

        public static void JoinGroup(this ICharacterEntity characterEntity, Group group)
        {
            characterEntity.Group = group;
            group.JoinGroup(characterEntity);
        }

        public static async Task LeaveGroupAsync(this ICharacterEntity characterEntity,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry)
        {
            characterEntity.Group!.LeaveGroup(characterEntity);
            foreach (var member in characterEntity.Group.Keys.Where(s => (s.Item2 != characterEntity.VisualId) || (s.Item1 != VisualType.Player)))
            {
                var groupMember = sessionRegistry.GetCharacter(s =>
                    (s.VisualId == member.Item2) && (member.Item1 == VisualType.Player));

                if (groupMember == null)
                {
                    continue;
                }

                if (characterEntity.Group.Count == 1)
                {
                    await groupMember.LeaveGroupAsync(sessionGroupFactory, sessionRegistry);
                    await groupMember.SendPacketAsync(characterEntity.Group.GeneratePidx(groupMember));
                    await groupMember.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.PartyDisbanded
                    });
                }

                await groupMember.SendPacketAsync(groupMember.Group!.GeneratePinit());
            }

            characterEntity.Group = new Group(GroupType.Group, sessionGroupFactory);
            characterEntity.Group.JoinGroup(characterEntity);
        }
    }
}
