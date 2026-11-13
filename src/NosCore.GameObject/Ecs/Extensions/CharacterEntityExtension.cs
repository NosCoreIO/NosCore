//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.DignityService;
using NosCore.GameObject.Ecs;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SkillPointService;
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
using NosCore.Packets.ServerPackets.Groups;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ShopService;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Data.Dto;
using NosCore.Dao.Interfaces;
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
using NosCore.GameObject.Services.SkillService;
using NosCore.Shared.Enumerations;
using NosCore.Core.I18N;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostedPacket = NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket;

namespace NosCore.GameObject.Ecs.Extensions
{
    public static class CharacterEntityExtension
    {
        // A system message the client shows as said by this character, which is how it tells
        // a player about their own actions.
        [Obsolete(
           "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
        public static IEnumerable<IPacket> GenerateInv(this PlayerComponentBundle characterEntity, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer)
        {
            var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket>() };
            var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket>() };
            var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket>() };
            var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket>() };
            var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket>() };
            var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket>() };
            var inv9 = new InvPacket { Type = PocketType.Mount, IvnSubPackets = new List<IvnSubPacket>() };
            var inv10 = new InvPacket { Type = PocketType.Raid, IvnSubPackets = new List<IvnSubPacket>() };

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
                                inv0.IvnSubPackets.Add(new IvnSubPacket
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
                            else if (inv.ItemInstance is BoxInstance box)
                            {
                                inv0.IvnSubPackets.Add(box.GenerateIvnSubPacket(PocketType.Equipment, inv.Slot));
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
                    case NoscorePocketType.FirstPartnerInventory:
                    case NoscorePocketType.SecondPartnerInventory:
                    case NoscorePocketType.ThirdPartnerInventory:
                        break;
                    default:
                        logger.LogInformation(
                            logLanguageLocalizer[LogLanguageKey.POCKETTYPE_UNKNOWN]);
                        break;
                }
            }

            return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7, inv9, inv10 };
        }

        // Passives and specialist card skills are free; only the class skills spend CP.
        public static int RemainingSkillPoints(this PlayerComponentBundle characterEntity, ISkillPointService skillPointService)
            => skillPointService.GetSkillPoints(characterEntity.Class, characterEntity.JobLevel)
               - characterEntity.Skills.Values
                   .Where(s => s.Skill != null && !s.Skill.IsPassive() && !s.Skill.IsSpecialist())
                   .Sum(s => s.Skill.CpCost);

        public static SkiPacket GenerateSki(this PlayerComponentBundle characterEntity, bool useSpecialist)
        {
            // The client takes the first listed skill for the basic attack, so passives go last.
            var ordered = characterEntity.Skills.Values
                .Where(s => s.Skill != null && s.Skill.IsSpecialist() == useSpecialist)
                .OrderBy(s => s.Skill.IsPassive())
                .ThenBy(s => s.Skill.CastId)
                .Select(s => s.SkillVNum)
                .ToList();

            var classByte = (byte)characterEntity.Class;
            var cardAttack = ordered.Count > 0 ? ordered[0] : (short)0;
            return new SkiPacket
            {
                PrimarySkillVnum = useSpecialist ? cardAttack : (short)(200 + 20 * classByte),
                SecondarySkillVnum = useSpecialist ? cardAttack : (short)(201 + 20 * classByte),
                SkillVnums = ordered,
            };
        }

        public static IEnumerable<QSlotPacket> GenerateQuicklist(this PlayerComponentBundle characterEntity)
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

        public static NosCore.Packets.ServerPackets.Player.RsfiPacket GenerateRsfi(this PlayerComponentBundle characterEntity)
        {
            return new NosCore.Packets.ServerPackets.Player.RsfiPacket
            {
                Act = 1,
                ActPart = 1,
                Unknown1 = 0,
                Unknown2 = 9,
                Ts = 0,
                TsMax = 9
            };
        }

        public static ServerExcListPacket GenerateServerExcListPacket(this PlayerComponentBundle aliveEntity, long? gold,
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

        public static ClPacket GenerateInvisible(this PlayerComponentBundle visualEntity)
        {
            return new ClPacket
            {
                VisualId = visualEntity.VisualId,
                Camouflage = visualEntity.Camouflage,
                Invisible = visualEntity.Invisible
            };
        }

        public static async Task SendFinfoAsync(this PlayerComponentBundle visualEntity, IFriendHub friendHttpClient,
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
                                   IsConnected = isConnected,
                                   CharacterName = visualEntity.Name
                               }
                           }
                       }
                    }),
                    ReceiverType = ReceiverType.OnlySomeone,
                    SenderCharacter = new Data.WebApi.Character { Id = visualEntity.VisualId, Name = visualEntity.Name },
                    ReceiverCharacter = new Data.WebApi.Character
                    {
                        Id = friend.CharacterId,
                        Name = friend.CharacterName
                    }
                })));
        }

        public static QstlistPacket GenerateQuestPacket(this PlayerComponentBundle visualEntity, bool showDialog = false)
        {
            return new QstlistPacket(visualEntity.Quests.Values
                .Where(s => s.CompletedOn == null).Select(quest => quest.GenerateQuestSubPacket(showDialog)).ToList());
        }

        public static InEquipmentSubPacket GetEquipmentSubPacket(this PlayerComponentBundle visualEntity) => new()
        {
            Armor = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            CostumeHat = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeHat, NoscorePocketType.Wear)
                ?.ItemInstance?.LookVNum(),
            CostumeSuit = visualEntity.InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeSuit, NoscorePocketType.Wear)
                ?.ItemInstance?.LookVNum(),
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

        public static Task AddGoldAsync(this PlayerComponentBundle characterEntity)
        {
            return characterEntity.SendPacketAsync(characterEntity.GenerateGold());
        }

        public static async Task AddGoldAsync(this PlayerComponentBundle characterEntity, long gold, IGameLanguageLocalizer localizer)
        {
            characterEntity.Gold += gold;
            await characterEntity.SendPacketAsync(characterEntity.GenerateGold());
            await characterEntity.SendPacketAsync(GenerateUpdateGoldSayPacket(characterEntity, localizer));
        }

        public static async Task RemoveGoldAsync(this PlayerComponentBundle characterEntity, long gold, IGameLanguageLocalizer localizer)
        {
            characterEntity.Gold -= gold;
            await characterEntity.SendPacketAsync(characterEntity.GenerateGold());
            await characterEntity.SendPacketAsync(GenerateUpdateGoldSayPacket(characterEntity, localizer));
        }

        private static SayPacket GenerateUpdateGoldSayPacket(PlayerComponentBundle characterEntity, IGameLanguageLocalizer localizer) =>
            characterEntity.GenerateSay(localizer[LanguageKey.UPDATE_GOLD, characterEntity.AccountLanguage], SayColorType.Red);

        public static async Task SetJobLevelAsync(this PlayerComponentBundle characterEntity, byte jobLevel,
            IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISkillPointService skillPointService)
        {
            characterEntity.JobLevel = (byte)((characterEntity.Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20 : jobLevel);
            characterEntity.JobLevelXp = 0;
            await characterEntity.SendPacketAsync(characterEntity.GenerateLev(experienceService, jobExperienceService, heroExperienceService, skillPointService));
            await characterEntity.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.JobLevelIncreased
            });
        }

        public static async Task SetHeroLevelAsync(this PlayerComponentBundle characterEntity, byte level,
            IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISkillPointService skillPointService)
        {
            characterEntity.HeroLevel = level;
            characterEntity.HeroLevelXp = 0;
            await characterEntity.SendPacketAsync(characterEntity.GenerateStat());
            await characterEntity.SendPacketAsync(characterEntity.GenerateStatInfo());
            await characterEntity.SendPacketAsync(characterEntity.GenerateLev(experienceService, jobExperienceService, heroExperienceService, skillPointService));
            await characterEntity.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.HeroLevelIncreased
            });
        }

        // Alone, the official server lists nobody; in a group the players come first. Either way
        // the recipient's own pets that are out follow.
        public static PinitPacket GeneratePinit(this PlayerComponentBundle character)
        {
            var group = character.Group;
            var entries = group is { Count: > 1 }
                ? group.GeneratePinit().PinitSubPackets ?? new List<PinitSubPacket?>()
                : new List<PinitSubPacket?>();
            if (character is PlayerComponentBundle player)
            {
                entries.AddRange(player.Mates.Values
                    .Where(s => s.IsTeamMember)
                    .Select(s => (PinitSubPacket?)s.GenerateSubPinit(player.AccountLanguage)));
            }

            return new PinitPacket { GroupSize = entries.Count, PinitSubPackets = entries };
        }

        public static void JoinGroup(this PlayerComponentBundle characterEntity, Group group)
        {
            characterEntity.Group = group;
            group.JoinGroup(characterEntity);
        }

        public static async Task LeaveGroupAsync(this PlayerComponentBundle characterEntity,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry)
        {
            characterEntity.Group.LeaveGroup(characterEntity);

            var leaving = characterEntity.VisualId;
            foreach (var memberId in characterEntity.Group.Keys
                         .Where(s => s.Item1 == VisualType.Player && s.Item2 != leaving).Select(s => s.Item2).ToList())
            {
                // Asked by id: a member leaving the world at the same moment has no entity left.
                if (!sessionRegistry.TryGetCharacter(s => s.VisualId == memberId, out var groupMember))
                {
                    continue;
                }

                await groupMember.SendPacketAsync(groupMember.GeneratePinit());
            }

            characterEntity.Group = new Group(GroupType.Group, sessionGroupFactory);
            characterEntity.Group.JoinGroup(characterEntity);
        }

        public static void AddBankGold(this PlayerComponentBundle characterEntity, long bankGold)
        {
            characterEntity.BankGold += bankGold;
        }

        public static void RemoveBankGold(this PlayerComponentBundle characterEntity, long bankGold)
        {
            characterEntity.BankGold -= bankGold;
        }

        public static async Task CloseShopAsync(this PlayerComponentBundle characterEntity)
        {
            if (characterEntity.Shop is { } shop)
            {
                using (await shop.Lock.AcquireAsync())
                {
                    CloseShop(characterEntity, shop);
                }
            }

            await SendShopClosedAsync(characterEntity);
        }

        private static void CloseShop(PlayerComponentBundle characterEntity, Shop shop)
        {
            shop.IsClosed = true;
            shop.DialogSubscription?.Dispose();
            shop.DialogSubscription = null;
            if (ReferenceEquals(characterEntity.Shop, shop))
            {
                characterEntity.Shop = null;
            }
        }

        private static async Task SendShopClosedAsync(PlayerComponentBundle characterEntity)
        {
            await characterEntity.MapInstance.SendPacketAsync(characterEntity.GenerateShop(characterEntity.AccountLanguage));
            await characterEntity.MapInstance.SendPacketAsync(characterEntity.GeneratePFlag());

            characterEntity.IsSitting = false;
            await characterEntity.SendPacketAsync(characterEntity.GenerateCond());
            await characterEntity.MapInstance.SendPacketAsync(characterEntity.GenerateRest());
        }

        public static async Task BuyAsync(this PlayerComponentBundle characterEntity, Shop shop, short slot, short amount,
            Microsoft.Extensions.Options.IOptions<NosCore.Core.Configuration.WorldConfiguration> worldConfiguration,
            IItemGenerationService itemProvider, IDignityService dignityService,
            IShellOptionService? shellOptionService = null,
            IDao<IItemInstanceDto?, Guid>? itemInstanceDao = null)
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

            var itemPrice = item.Price ?? item.ItemInstance.Item.Price;
            if (itemPrice < 0 || itemPrice > long.MaxValue / amount)
            {
                return;
            }
            var price = itemPrice * amount;

            var itemReputPrice = item.Price == null ? item.ItemInstance.Item.ReputPrice : 0;
            if (itemReputPrice < 0 || itemReputPrice > long.MaxValue / amount)
            {
                return;
            }
            var reputprice = itemReputPrice * amount;

            var percent = dignityService.GetShopPriceMultiplier(characterEntity.DignityIcon);
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

            if (reputprice > characterEntity.Reputation)
            {
                await characterEntity.SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.FailNpc,
                    Message = Game18NConstString.ReputationNotHighEnough
                });
                return;
            }

            if (shop.OwnerCharacter is { } owner)
            {
                await BuyFromPlayerShopAsync(characterEntity, shop, owner, slot, amount, (long)(price * percent), price,
                    worldConfiguration, shellOptionService, itemInstanceDao);
                return;
            }

            var inv = characterEntity.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                itemProvider.Create(item.ItemInstance.ItemVNum, amount), characterEntity.CharacterId));

            if (inv?.Count > 0)
            {
                inv.ForEach(it => it.CharacterId = characterEntity.CharacterId);
                await characterEntity.SendPacketsAsync(inv.Select(invItem => invItem.GeneratePocketChange(invItem.Type.ToPocketType(), invItem.Slot)));
                await characterEntity.SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.SuccessNpc,
                    Message = Game18NConstString.TradeSuccessfull
                });

                if (reputprice == 0)
                {
                    characterEntity.Gold -= (long)(price * percent);
                    await characterEntity.SendPacketAsync(characterEntity.GenerateGold());
                }
                else
                {
                    characterEntity.Reputation -= reputprice;
                    await characterEntity.SendPacketAsync(characterEntity.GenerateFd());
                    await characterEntity.SendPacketAsync(characterEntity.GenerateSayi(SayColorType.Red, Game18NConstString.ReputationReduced,
                        4, reputprice));
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

        private static async Task BuyFromPlayerShopAsync(PlayerComponentBundle buyer, Shop shop, PlayerComponentBundle owner, short slot, short amount,
            long cost, long sellAmount, Microsoft.Extensions.Options.IOptions<NosCore.Core.Configuration.WorldConfiguration> worldConfiguration,
            IShellOptionService? shellOptionService, IDao<IItemInstanceDto?, Guid>? itemInstanceDao)
        {
            if (owner.CharacterId == buyer.CharacterId)
            {
                return;
            }

            IPacket? failure = null;
            var sold = false;
            var closed = false;
            InventoryItemInstance? sellerSlot = null;
            NoscorePocketType sellerType = default;
            short sellerSlotIndex = 0;
            List<InventoryItemInstance>? delivered = null;
            IItemInstance? persisted = null;
            using (await shop.Lock.AcquireAsync())
            {
                var item = shop.ShopItems.Values.FirstOrDefault(it => it.Slot == slot);
                if (shop.IsClosed || item?.ItemInstance == null || amount > item.Amount)
                {
                    return;
                }

                if (cost > buyer.Gold)
                {
                    failure = new SMemoiPacket { Type = SMemoType.FailNpc, Message = Game18NConstString.NotEnoughGold5 };
                }
                else if (sellAmount + owner.Gold > worldConfiguration.Value.MaxGoldAmount)
                {
                    failure = new SMemoiPacket { Type = SMemoType.FailPlayer, Message = Game18NConstString.TradeFailedMaxMoney };
                }
                else if (!owner.InventoryService.TryGetValue(item.ItemInstance.Id, out var live)
                    || live.ItemInstance == null || live.ItemInstance.ItemVNum != item.ItemInstance.ItemVNum
                    || live.ItemInstance.Amount < amount)
                {
                    return;
                }
                else
                {
                    // The buyer gets a copy under a new id: an instance id belongs to exactly one inventory.
                    var copy = (IItemInstance)live.ItemInstance.Clone();
                    copy.Id = Guid.NewGuid();
                    copy.Amount = amount;
                    if (copy is WearableInstance wearable)
                    {
                        // Options are stored per instance id; the copy gets its own rows below.
                        wearable.EquipmentOptions = null;
                        if ((shellOptionService != null) && (itemInstanceDao != null))
                        {
                            var carried = false;
                            try
                            {
                                carried = (await itemInstanceDao.TryInsertOrUpdateAsync(copy) != null)
                                    && await shellOptionService.CopyAsync(live.ItemInstance.Id, copy.Id);
                            }
                            catch (Exception)
                            {
                                carried = false;
                            }

                            if (!carried)
                            {
                                await itemInstanceDao.TryDeleteAsync(copy.Id);
                                return;
                            }

                            persisted = copy;
                        }
                    }

                    buyer.Gold -= cost;
                    var wholeStack = live.ItemInstance.Amount == amount;
                    if (wholeStack)
                    {
                        owner.InventoryService.Remove(live.ItemInstanceId);
                    }
                    else
                    {
                        live.ItemInstance.Amount -= amount;
                    }

                    delivered = buyer.InventoryService.AddItemToPocket(
                        InventoryItemInstance.Create(copy, buyer.CharacterId));
                    if (delivered is not { Count: > 0 })
                    {
                        if (wholeStack)
                        {
                            owner.InventoryService[live.ItemInstanceId] = live;
                        }
                        else
                        {
                            live.ItemInstance.Amount += amount;
                        }

                        buyer.Gold += cost;
                        if (persisted != null && itemInstanceDao != null)
                        {
                            await itemInstanceDao.TryDeleteAsync(persisted.Id);
                        }

                        failure = new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.NotEnoughSpace };
                    }
                    else
                    {
                        owner.Gold += sellAmount;
                        shop.Sell += sellAmount;
                        item.Amount = (short)((item.Amount ?? 0) - amount);
                        if (item.Amount <= 0)
                        {
                            shop.ShopItems.TryRemove(item.Slot, out _);
                        }

                        sellerSlot = wholeStack ? null : live;
                        sellerType = live.Type;
                        sellerSlotIndex = live.Slot;
                        sold = true;
                        if (shop.ShopItems.IsEmpty)
                        {
                            CloseShop(owner, shop);
                            closed = true;
                        }
                    }
                }
            }

            if (failure != null)
            {
                await buyer.SendPacketAsync(failure);
                return;
            }

            if (!sold || delivered == null)
            {
                return;
            }

            await owner.SendPacketAsync(sellerSlot.GeneratePocketChange(sellerType.ToPocketType(), sellerSlotIndex));
            await owner.SendPacketAsync(owner.GenerateGold());
            if (closed)
            {
                await SendShopClosedAsync(owner);
            }
            else
            {
                await buyer.SendPacketAsync(owner.GenerateNInv(shop, 1, 0));
            }

            await buyer.SendPacketsAsync(delivered.Select(invItem => invItem.GeneratePocketChange(invItem.Type.ToPocketType(), invItem.Slot)));
            await buyer.SendPacketAsync(new SMemoiPacket
            {
                Type = SMemoType.SuccessNpc,
                Message = Game18NConstString.TradeSuccessfull
            });
            await buyer.SendPacketAsync(buyer.GenerateGold());
        }
    }
}
