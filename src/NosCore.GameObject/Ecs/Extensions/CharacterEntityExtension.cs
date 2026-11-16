//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ShopService;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Data.Dto;
using NosCore.Dao.Interfaces;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.GameObject.Services.SkillService;
using NosCore.Shared.Enumerations;
using NosCore.Core.I18N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostedPacket = NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket;

namespace NosCore.GameObject.Ecs.Extensions
{
    public static class CharacterEntityExtension
    {
        // Passives and specialist card skills are free; only the class skills spend CP.
        public static int RemainingSkillPoints(this PlayerComponentBundle characterEntity, ISkillPointService skillPointService)
            => skillPointService.GetSkillPoints(characterEntity.Class, characterEntity.JobLevel)
               - characterEntity.Skills.Values
                   .Where(s => s.Skill != null && !s.Skill.IsPassive() && !s.Skill.IsSpecialist())
                   .Sum(s => s.Skill.CpCost);

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

        public static InEquipmentSubPacket GetEquipmentSubPacket(this PlayerComponentBundle visualEntity) => new()
        {
            Armor = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.Armor, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            CostumeHat = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.CostumeHat, NoscorePocketType.Wear)
                ?.ItemInstance?.LookVNum(),
            CostumeSuit = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.CostumeSuit, NoscorePocketType.Wear)
                ?.ItemInstance?.LookVNum(),
            Fairy = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            Hat = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.Hat, NoscorePocketType.Wear)?.ItemInstance?.ItemVNum,
            MainWeapon = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.MainWeapon, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            Mask = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.Mask, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            SecondaryWeapon = visualEntity.InventoryService
                .LoadBySlotAndType(EquipmentType.SecondaryWeapon, NoscorePocketType.Wear)?.ItemInstance?
                .ItemVNum,
            WeaponSkin = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.WeaponSkin, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            WingSkin = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.WingSkin, NoscorePocketType.Wear)
                ?.ItemInstance?.ItemVNum,
            MiniPet = visualEntity.InventoryService.LoadBySlotAndType(EquipmentType.MiniPet, NoscorePocketType.Wear)
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
            IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISpExperienceService spExperienceService, ISkillPointService skillPointService)
        {
            characterEntity.JobLevel = (byte)((characterEntity.Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20 : jobLevel);
            characterEntity.JobLevelXp = 0;
            await characterEntity.SendPacketAsync(characterEntity.GenerateLev(experienceService, jobExperienceService, heroExperienceService, spExperienceService, skillPointService));
            await characterEntity.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.JobLevelIncreased
            });
        }

        public static async Task SetHeroLevelAsync(this PlayerComponentBundle characterEntity, byte level,
            IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISpExperienceService spExperienceService, ISkillPointService skillPointService)
        {
            characterEntity.HeroLevel = level;
            characterEntity.HeroLevelXp = 0;
            await characterEntity.SendPacketAsync(characterEntity.GenerateStat());
            await characterEntity.SendPacketAsync(characterEntity.GenerateStatInfo());
            await characterEntity.SendPacketAsync(characterEntity.GenerateLev(experienceService, jobExperienceService, heroExperienceService, spExperienceService, skillPointService));
            await characterEntity.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.HeroLevelIncreased
            });
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

            var percent = shop.OwnerCharacter == null ? dignityService.GetShopPriceMultiplier(characterEntity.DignityIcon,
                characterEntity.MapInstance.IsAct4) : 1;
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

            var bought = item.ItemInstance.Item.Type is NoscorePocketType.Main or NoscorePocketType.Etc
                ? [itemProvider.Create(item.ItemInstance.ItemVNum, amount)]
                : Enumerable.Range(0, amount).Select(_ => itemProvider.Create(item.ItemInstance.ItemVNum, 1)).ToList();
            var inv = characterEntity.InventoryService.HasRoomFor(bought)
                ? bought.SelectMany(it => characterEntity.InventoryService.AddItemToPocket(
                    InventoryItemInstance.Create(it, characterEntity.CharacterId)) ?? []).ToList()
                : null;

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
            SellListSubPacket? soldLine = null;
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
                            catch
                            {
                                _ = await itemInstanceDao.TryDeleteAsync(copy.Id);
                                throw;
                            }

                            if (!carried)
                            {
                                _ = await itemInstanceDao.TryDeleteAsync(copy.Id);
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
                            _ = await itemInstanceDao.TryDeleteAsync(persisted.Id);
                        }

                        failure = new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.NotEnoughSpace };
                    }
                    else
                    {
                        owner.Gold += sellAmount;
                        shop.Sell += sellAmount;
                        item.Amount = (short)((item.Amount ?? 0) - amount);
                        soldLine = new SellListSubPacket { Slot = item.Slot, Amount = amount, SellAmount = item.Amount ?? 0 };
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
            await owner.SendPacketAsync(new SellListPacket { ValueSold = shop.Sell, SellListSubPacket = [soldLine] });
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
