//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ShopService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Core.I18N;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostedPacket = NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket;

namespace NosCore.GameObject.Ecs.Extensions
{
    public static class CharacterEntityExtension
    {
        [Obsolete(
           "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
        public static IEnumerable<IPacket> GenerateInv(this ICharacterEntity characterEntity, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer)
        {
            var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket>() };
            var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket>() };
            var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket>() };
            var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket>() };
            var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket>() };
            var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket>() };
            var inv9 = new InvPacket { Type = (PocketType)NoscorePocketType.Mount, IvnSubPackets = new List<IvnSubPacket>() };
            var inv10 = new InvPacket { Type = (PocketType)NoscorePocketType.Raid, IvnSubPackets = new List<IvnSubPacket>() };

            if (characterEntity.InventoryService == null)
            {
                return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7, inv9, inv10 };
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

                    case NoscorePocketType.Mount:
                        inv9.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                        });
                        break;

                    case NoscorePocketType.Raid:
                        inv10.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                        });
                        break;

                    case NoscorePocketType.Wear:
                        break;
                    default:
                        logger.Information(
                            logLanguageLocalizer[LogLanguageKey.POCKETTYPE_UNKNOWN]);
                        break;
                }
            }

            return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7, inv9, inv10 };
        }

        public static SkiPacket GenerateSki(this ICharacterEntity characterEntity)
        {
            List<CharacterSkill> characterSkills = characterEntity.Skills.Values.OrderBy(s => s.Skill?.CastId).ToList();
            var spStarter = characterSkills.FirstOrDefault()?.SkillVNum ?? 0;
            return new SkiPacket
            {
                PrimarySkillVnum = (short)(!characterEntity.UseSp ? 201 + 20 * (byte)characterEntity.Class : spStarter),
                SecondarySkillVnum = (short)(!characterEntity.UseSp ? 200 + 20 * (byte)characterEntity.Class : spStarter),
                SkillVnums = characterSkills.Select(x => (short)x.SkillVNum).ToList(),
            };
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

        public static ClPacket GenerateInvisible(this ICharacterEntity visualEntity)
        {
            return new ClPacket
            {
                VisualId = visualEntity.VisualId,
                Camouflage = visualEntity.Camouflage,
                Invisible = visualEntity.Invisible
            };
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

        public static QstlistPacket GenerateQuestPacket(this ICharacterEntity visualEntity, bool showDialog = false)
        {
            return new QstlistPacket(visualEntity.Quests.Values
                .Where(s => s.CompletedOn == null).Select(quest => quest.GenerateQuestSubPacket(showDialog)).ToList());
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

        private static GoldPacket GenerateGoldPacket(ICharacterEntity characterEntity) => new() { Gold = characterEntity.Gold };

        private static LevPacket GenerateLevPacket(ICharacterEntity characterEntity, IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
            => new()
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

        private static StatPacket GenerateStatPacket(ICharacterEntity characterEntity) => new()
        {
            Hp = characterEntity.Hp,
            HpMaximum = characterEntity.MaxHp,
            Mp = characterEntity.Mp,
            MpMaximum = characterEntity.MaxMp,
            Unknown = 0,
            Option = 0
        };

        private static FdPacket GenerateFdPacket(ICharacterEntity characterEntity) => new()
        {
            Reput = characterEntity.Reput,
            Dignity = characterEntity.Dignity,
            ReputIcon = (int)characterEntity.ReputIcon,
            DignityIcon = (int)characterEntity.DignityIcon
        };

        public static Task AddGoldAsync(this ICharacterEntity characterEntity)
        {
            return characterEntity.SendPacketAsync(GenerateGoldPacket(characterEntity));
        }

        public static async Task AddGoldAsync(this ICharacterEntity characterEntity, long gold, IGameLanguageLocalizer localizer)
        {
            characterEntity.Gold += gold;
            await characterEntity.SendPacketAsync(GenerateGoldPacket(characterEntity));
            await characterEntity.SendPacketAsync(GenerateUpdateGoldSayPacket(characterEntity, localizer));
        }

        public static async Task RemoveGoldAsync(this ICharacterEntity characterEntity, long gold, IGameLanguageLocalizer localizer)
        {
            characterEntity.Gold -= gold;
            await characterEntity.SendPacketAsync(GenerateGoldPacket(characterEntity));
            await characterEntity.SendPacketAsync(GenerateUpdateGoldSayPacket(characterEntity, localizer));
        }

        private static SayPacket GenerateUpdateGoldSayPacket(ICharacterEntity characterEntity, IGameLanguageLocalizer localizer)
        {
            return new SayPacket
            {
                VisualType = VisualType.Player,
                VisualId = characterEntity.VisualId,
                Type = SayColorType.Red,
                Message = localizer[LanguageKey.UPDATE_GOLD, characterEntity.AccountLanguage]
            };
        }

        public static async Task SetJobLevelAsync(this ICharacterEntity characterEntity, byte jobLevel,
            IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService)
        {
            characterEntity.JobLevel = (byte)((characterEntity.Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20 : jobLevel);
            characterEntity.JobLevelXp = 0;
            await characterEntity.SendPacketAsync(GenerateLevPacket(characterEntity, experienceService, jobExperienceService, heroExperienceService));
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
            await characterEntity.SendPacketAsync(GenerateStatPacket(characterEntity));
            await characterEntity.SendPacketAsync(characterEntity.GenerateStatInfo());
            await characterEntity.SendPacketAsync(GenerateLevPacket(characterEntity, experienceService, jobExperienceService, heroExperienceService));
            await characterEntity.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.HeroLevelIncreased
            });
        }

        public static void JoinGroup(this ICharacterEntity characterEntity, Group group)
        {
            characterEntity.Group = group;
            group.JoinGroup(characterEntity);
        }

        public static async Task LeaveGroupAsync(this ICharacterEntity characterEntity,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry)
        {
            characterEntity.Group.LeaveGroup(characterEntity);

            foreach (var entry in characterEntity.Group.Values.Where(s =>
                s.Item2.VisualType == VisualType.Player && s.Item2.VisualId != characterEntity.VisualId))
            {
                if (entry.Item2 is not ICharacterEntity groupMember)
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

                await groupMember.SendPacketAsync(groupMember.Group.GeneratePinit());
            }

            characterEntity.Group = new Group(GroupType.Group, sessionGroupFactory);
            characterEntity.Group.JoinGroup(characterEntity);
        }

        public static void AddBankGold(this ICharacterEntity characterEntity, long bankGold)
        {
            characterEntity.BankGold += bankGold;
        }

        public static void RemoveBankGold(this ICharacterEntity characterEntity, long bankGold)
        {
            characterEntity.BankGold -= bankGold;
        }

        public static async Task CloseShopAsync(this ICharacterEntity characterEntity)
        {
            characterEntity.Shop = null;

            await characterEntity.MapInstance.SendPacketAsync(characterEntity.GenerateShop(characterEntity.AccountLanguage));
            await characterEntity.MapInstance.SendPacketAsync(characterEntity.GeneratePFlag());

            characterEntity.IsSitting = false;
            await characterEntity.SendPacketAsync(characterEntity.GenerateCond());
            await characterEntity.MapInstance.SendPacketAsync(characterEntity.GenerateRest());
        }

        public static async Task BuyAsync(this ICharacterEntity characterEntity, Shop shop, short slot, short amount,
            Microsoft.Extensions.Options.IOptions<NosCore.Core.Configuration.WorldConfiguration> worldConfiguration,
            IItemGenerationService itemProvider,
            IGameLanguageLocalizer localizer)
        {
            if (amount <= 0)
            {
                return;
            }

            var item = shop.ShopItems.Values.FirstOrDefault(it => it.Slot == slot);
            if (item == null)
            {
                return;
            }

            var itemPrice = item.Price ?? item.ItemInstance!.Item.Price;
            if (itemPrice < 0 || itemPrice > long.MaxValue / amount)
            {
                return;
            }
            var price = itemPrice * amount;

            var itemReputPrice = item.Price == null ? item.ItemInstance!.Item.ReputPrice : 0;
            if (itemReputPrice < 0 || itemReputPrice > long.MaxValue / amount)
            {
                return;
            }
            var reputprice = itemReputPrice * amount;

            var percent = characterEntity.DignityIcon switch
            {
                DignityType.Dreadful => 1.1,
                DignityType.Unqualified => 1.2,
                DignityType.Failed => 1.5,
                DignityType.Useless => 1.5,
                _ => 1.0,
            };
            if (amount > item.Amount)
            {
                return;
            }

            if ((reputprice == 0) && (price * percent > characterEntity.Gold))
            {
                await characterEntity.SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.FailNpc,
                    Message = Game18NConstString.NotEnoughGold5
                });
                return;
            }

            if (reputprice > characterEntity.Reput)
            {
                await characterEntity.SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.FailNpc,
                    Message = Game18NConstString.ReputationNotHighEnough
                });
                return;
            }

            short slotChar = item.Slot;
            List<InventoryItemInstance>? inv;
            if (shop.OwnerCharacter == null)
            {
                inv = characterEntity.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                    itemProvider.Create(item.ItemInstance!.ItemVNum, amount), characterEntity.CharacterId));
            }
            else
            {
                if (price + shop.OwnerCharacter.Gold > worldConfiguration.Value.MaxGoldAmount)
                {
                    await characterEntity.SendPacketAsync(new SMemoPacket
                    {
                        Type = SMemoType.FailPlayer,
                        Message = localizer[LanguageKey.TOO_RICH_SELLER, characterEntity.AccountLanguage]
                    });
                    return;
                }

                if (amount == item.ItemInstance?.Amount)
                {
                    inv = characterEntity.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item.ItemInstance,
                        characterEntity.CharacterId));
                }
                else
                {
                    inv = characterEntity.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                        itemProvider.Create(item.ItemInstance?.ItemVNum ?? 0, amount), characterEntity.CharacterId));
                }
            }

            if (inv?.Count > 0)
            {
                inv.ForEach(it => it.CharacterId = characterEntity.CharacterId);
                var packet = await (shop.OwnerCharacter == null ? Task.FromResult((NInvPacket?)null) : BuyFromOwnerAsync(shop.OwnerCharacter, item, amount, slotChar));
                if (packet != null)
                {
                    await characterEntity.SendPacketAsync(packet);
                }

                await characterEntity.SendPacketsAsync(inv.Select(invItem => invItem.GeneratePocketChange((PocketType)invItem.Type, invItem.Slot)));
                await characterEntity.SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.SuccessNpc,
                    Message = Game18NConstString.TradeSuccessfull
                });

                if (reputprice == 0)
                {
                    characterEntity.Gold -= (long)(price * percent);
                    await characterEntity.SendPacketAsync(GenerateGoldPacket(characterEntity));
                }
                else
                {
                    characterEntity.Reput -= reputprice;
                    await characterEntity.SendPacketAsync(GenerateFdPacket(characterEntity));
                    await characterEntity.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = characterEntity.CharacterId,
                        Type = SayColorType.Red,
                        Message = Game18NConstString.ReputationReduced,
                        ArgumentType = 4,
                        Game18NArguments = { reputprice }
                    });
                }
            }
            else
            {
                await characterEntity.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
            }
        }

        private static async Task<NInvPacket?> BuyFromOwnerAsync(ICharacterEntity characterEntity, ShopItem item, short amount, short slotChar)
        {
            var type = item.Type;
            var itemInstance = amount == item.ItemInstance?.Amount
                ? characterEntity.InventoryService.DeleteById(item.ItemInstance.Id)
                : characterEntity.InventoryService.RemoveItemAmountFromInventory(amount, item.ItemInstance!.Id);
            var slot = item.Slot;
            item.Amount = (short)((item.Amount ?? 0) - amount);
            if ((item?.Amount ?? 0) == 0)
            {
                characterEntity.Shop!.ShopItems.TryRemove(slot, out _);
            }

            await characterEntity.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)type, slotChar));
            var sellAmount = (item?.Price ?? 0) * amount;
            characterEntity.Gold += sellAmount;
            await characterEntity.SendPacketAsync(GenerateGoldPacket(characterEntity));
            characterEntity.Shop!.Sell += sellAmount;

            if (!characterEntity.Shop.ShopItems.IsEmpty)
            {
                return characterEntity.GenerateNInv(1, 0);
            }

            await characterEntity.CloseShopAsync();
            return null;
        }
    }
}
