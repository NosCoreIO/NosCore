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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using DotNetty.Transport.Channels;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MinilandProvider;
using Serilog;

namespace NosCore.GameObject
{
    public class Character : CharacterDto, ICharacterEntity
    {
        private readonly IGenericDao<AccountDto>? _accountDao;
        private readonly IGenericDao<CharacterDto>? _characterDao;
        private readonly IGenericDao<InventoryItemInstanceDto>? _inventoryItemInstanceDao;
        private readonly IGenericDao<IItemInstanceDto>? _itemInstanceDao;
        private readonly ILogger _logger;
        private readonly IGenericDao<MinilandDto> _minilandDao;
        private readonly IMinilandProvider _minilandProvider;
        private readonly IGenericDao<QuicklistEntryDto> _quicklistEntriesDao;

        private readonly IGenericDao<StaticBonusDto> _staticBonusDao;
        private readonly IGenericDao<TitleDto> _titleDao;
        private byte _speed;

        public Character(IInventoryService inventory, IExchangeProvider exchangeProvider, IItemProvider itemProvider,
            IGenericDao<CharacterDto> characterDao, IGenericDao<IItemInstanceDto> itemInstanceDao,
            IGenericDao<InventoryItemInstanceDto> inventoryItemInstanceDao, IGenericDao<AccountDto> accountDao,
            ILogger logger, IGenericDao<StaticBonusDto> staticBonusDao,
            IGenericDao<QuicklistEntryDto> quicklistEntriesDao, IGenericDao<MinilandDto> minilandDao,
            IMinilandProvider minilandProvider, IGenericDao<TitleDto> titleDao)
        {
            InventoryService = inventory;
            ExchangeProvider = exchangeProvider;
            ItemProvider = itemProvider;
            GroupRequestCharacterIds = new ConcurrentDictionary<long, long>();
            Group = new Group(GroupType.Group);
            Requests = new Subject<RequestData>();
            _characterDao = characterDao;
            _itemInstanceDao = itemInstanceDao;
            _accountDao = accountDao;
            _logger = logger;
            _inventoryItemInstanceDao = inventoryItemInstanceDao;
            _staticBonusDao = staticBonusDao;
            _titleDao = titleDao;
            QuicklistEntries = new List<QuicklistEntryDto>();
            _quicklistEntriesDao = quicklistEntriesDao;
            _minilandDao = minilandDao;
            _minilandProvider = minilandProvider;
        }

        public bool IsChangingMapInstance { get; set; }

        public DateTime LastPortal { get; set; }

        public ClientSession Session { get; set; }

        public DateTime LastSpeedChange { get; set; }

        public DateTime LastMove { get; set; }
        public IItemProvider? ItemProvider { get; set; }
        public bool InExchangeOrTrade { get; set; }

        public bool UseSp { get; set; }

        public DateTime LastSp { get; set; } = SystemTime.Now();
        public short SpCooldown { get; set; }
        public bool IsVehicled { get; set; }
        public byte? VehicleSpeed { get; set; }

        public IExchangeProvider? ExchangeProvider { get; }

        public bool InExchangeOrShop => InExchange || InShop;

        public bool InExchange => ExchangeProvider.CheckExchange(VisualId);

        public bool InShop { get; set; }
        public List<QuicklistEntryDto> QuicklistEntries { get; set; }

        public long BankGold => Account.BankMoney;

        public RegionType AccountLanguage => Account.Language;

        public ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; set; }
        public Subject<RequestData> Requests { get; set; }

        public short Race => (byte)Class;
        public Shop? Shop { get; set; }

        public bool Camouflage { get; set; }

        public bool Invisible { get; set; }

        public IInventoryService? InventoryService { get; }

        public Group Group { get; set; }

        /// <summary>
        /// Date of last group request sent
        /// </summary>
        public DateTime? LastGroupRequest { get; set; } = null;

        public int ReputIcon => GetReputIco();

        public int DignityIcon => GetDignityIco();

        public IChannel Channel => Session?.Channel;

        public Task SendPacket(IPacket packetDefinition)
        {
            return Session.SendPacket(packetDefinition);
        }

        public Task SendPackets(IEnumerable<IPacket> packetDefinitions)
        {
            return Session.SendPackets(packetDefinitions);
        }

        public MapInstance? MapInstance { get; set; }

        public VisualType VisualType => VisualType.Player;

        public short VNum { get; set; }

        public long VisualId => CharacterId;

        public byte Direction { get; set; }

        public byte Size { get; set; } = 10;

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public byte Speed
        {
            get
            {
                if (VehicleSpeed != null)
                {
                    return (byte)VehicleSpeed;
                }
                //    if (HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
                //    {
                //        return 0;
                //    }

                const int
                    bonusSpeed = 0; /*(byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.SetMovementNegated)[0];*/
                if (_speed + bonusSpeed > 59)
                {
                    return 59;
                }

                return (byte)(_speed + bonusSpeed);
            }

            set
            {
                LastSpeedChange = SystemTime.Now();
                _speed = value > 59 ? (byte)59 : value;
            }
        }

        public short Morph { get; set; }

        public byte MorphUpgrade { get; set; }

        public short MorphDesign { get; set; }

        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }

        public bool NoMove { get; set; }
        public bool IsSitting { get; set; }
        public Guid MapInstanceId { get; set; }

        public AuthorityType Authority => Account.Authority;

        public bool IsAlive { get; set; }

        public int MaxHp => (int)HpLoad();

        public int MaxMp => (int)MpLoad();

        public async Task SetHeroLevel(byte level)
        {
            HeroLevel = level;
            HeroXp = 0;
            await GenerateLevelupPackets();
            await SendPacket(new MsgPacket
            {
                Type = MessageType.White,
                Message = Language.Instance.GetMessageFromKey(LanguageKey.HERO_LEVEL_CHANGED, Session.Account.Language)
            });
        }

        public async Task SetJobLevel(byte jobLevel)
        {
            JobLevel = (byte)((Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20
                : jobLevel);
            JobLevelXp = 0;
            await SendPacket(GenerateLev());
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);
            Parallel.ForEach(mapSessions, s =>
            {
                //if (s.VisualId != VisualId)
                //{
                //    TODO: Generate GIDX
                //}

                s.SendPacket(this.GenerateEff(8));
            });
            await SendPacket(new MsgPacket
            {
                Type = MessageType.White,
                Message = Language.Instance.GetMessageFromKey(LanguageKey.JOB_LEVEL_CHANGED, Session.Account.Language)
            });
        }

        public void JoinGroup(Group group)
        {
            Group = group;
            group.JoinGroup(this);
        }

        public void LoadExpensions()
        {
            var backpack = StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BackPack);
            var backpackticket = StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.InventoryTicketUpgrade);
            var expension = (byte)((backpack ? 12 : 0) + (backpackticket ? 60 : 0));

            InventoryService.Expensions[NoscorePocketType.Main] += expension;
            InventoryService.Expensions[NoscorePocketType.Equipment] += expension;
            InventoryService.Expensions[NoscorePocketType.Etc] += expension;
        }

        public async Task LeaveGroup()
        {
            Group.LeaveGroup(this);
            foreach (var member in Group.Keys.Where(s => (s.Item2 != CharacterId) || (s.Item1 != VisualType.Player)))
            {
                var groupMember = Broadcaster.Instance.GetCharacter(s =>
                    (s.VisualId == member.Item2) && (member.Item1 == VisualType.Player));

                if (groupMember != null)
                {
                    if (Group.Count == 1)
                    {
                        await groupMember.LeaveGroup();
                        await groupMember.SendPacket(Group.GeneratePidx(groupMember));
                        await groupMember.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_CLOSED,
                                groupMember.AccountLanguage),
                            Type = MessageType.White
                        });
                    }

                    await groupMember.SendPacket(groupMember.Group.GeneratePinit());
                }
            }

            Group = new Group(GroupType.Group);
            Group.JoinGroup(this);
        }

        public void Save()
        {
            try
            {
                var account = Session.Account;
                _accountDao.InsertOrUpdate(ref account);

                CharacterDto character = (Character)MemberwiseClone();
                _characterDao.InsertOrUpdate(ref character);

                var quicklistEntriesToDelete = _quicklistEntriesDao
                    .Where(i => i.CharacterId == CharacterId).ToList()
                    .Where(i => QuicklistEntries.All(o => o.Id != i.Id)).ToList();
                _quicklistEntriesDao.Delete(quicklistEntriesToDelete.Select(s => s.Id).ToArray());
                _quicklistEntriesDao.InsertOrUpdate(QuicklistEntries);

                // load and concat inventory with equipment
                var itemsToDelete = _inventoryItemInstanceDao
                    .Where(i => i.CharacterId == CharacterId).ToList()
                    .Where(i => InventoryService.Values.All(o => o.Id != i.Id)).ToList();

                _inventoryItemInstanceDao.Delete(itemsToDelete);
                _itemInstanceDao.Delete(itemsToDelete.Select(s => s.ItemInstanceId).ToArray());

                _itemInstanceDao.InsertOrUpdate(InventoryService.Values.Select(s => s.ItemInstance).ToArray());
                _inventoryItemInstanceDao.InsertOrUpdate(InventoryService.Values.ToArray());

                var staticBonusToDelete = _staticBonusDao
                    .Where(i => i.CharacterId == CharacterId).ToList()
                    .Where(i => StaticBonusList.All(o => o.StaticBonusId != i.StaticBonusId)).ToList();
                _staticBonusDao.Delete(staticBonusToDelete);
                _staticBonusDao.InsertOrUpdate(StaticBonusList);

                _titleDao.InsertOrUpdate(Titles);

                var minilandDto = (MinilandDto)_minilandProvider.GetMiniland(CharacterId);
                _minilandDao.InsertOrUpdate(ref minilandDto);
            }
            catch (Exception e)
            {
                _logger.Error("Save Character failed. SessionId: " + Session.SessionId, e);
            }
        }

        public InEquipmentSubPacket Equipment => new InEquipmentSubPacket
        {
            Armor = InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear)?.ItemInstance
                .ItemVNum,
            CostumeHat = InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeHat, NoscorePocketType.Wear)
                ?.ItemInstance.ItemVNum,
            CostumeSuit = InventoryService.LoadBySlotAndType((short)EquipmentType.CostumeSuit, NoscorePocketType.Wear)
                ?.ItemInstance.ItemVNum,
            Fairy = InventoryService.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance
                .ItemVNum,
            Hat = InventoryService.LoadBySlotAndType((short)EquipmentType.Hat, NoscorePocketType.Wear)?.ItemInstance.ItemVNum,
            MainWeapon = InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear)
                ?.ItemInstance.ItemVNum,
            Mask = InventoryService.LoadBySlotAndType((short)EquipmentType.Mask, NoscorePocketType.Wear)?.ItemInstance
                .ItemVNum,
            SecondaryWeapon = InventoryService
                .LoadBySlotAndType((short)EquipmentType.SecondaryWeapon, NoscorePocketType.Wear)?.ItemInstance
                .ItemVNum,
            WeaponSkin = InventoryService.LoadBySlotAndType((short)EquipmentType.WeaponSkin, NoscorePocketType.Wear)
                ?.ItemInstance.ItemVNum,
            WingSkin = InventoryService.LoadBySlotAndType((short)EquipmentType.WingSkin, NoscorePocketType.Wear)
                ?.ItemInstance.ItemVNum
        };

        public UpgradeRareSubPacket WeaponUpgradeRareSubPacket
        {
            get
            {
                var weapon =
                    InventoryService.LoadBySlotAndType((short)EquipmentType.MainWeapon, NoscorePocketType.Wear);
                return new UpgradeRareSubPacket
                {
                    Upgrade = weapon?.ItemInstance.Upgrade ?? 0,
                    Rare = (sbyte)(weapon?.ItemInstance.Rare ?? 0)
                };
            }
        }

        public UpgradeRareSubPacket ArmorUpgradeRareSubPacket
        {
            get
            {
                var armor = InventoryService.LoadBySlotAndType((short)EquipmentType.Armor, NoscorePocketType.Wear);
                return new UpgradeRareSubPacket
                {
                    Upgrade = armor?.ItemInstance.Upgrade ?? 0,
                    Rare = (sbyte)(armor?.ItemInstance.Rare ?? 0)
                };
            }
        }

        public List<StaticBonusDto> StaticBonusList { get; set; }
        public List<TitleDto> Titles { get; set; }
        public bool IsDisconnecting { get; internal set; }

        public async Task ChangeClass(CharacterClassType classType)
        {
            if (Class == classType)
            {
                _logger.Error(
                    Language.Instance.GetMessageFromKey(LanguageKey.CANT_CHANGE_SAME_CLASS, Account.Language));
                return;
            }

            JobLevel = 1;
            JobLevelXp = 0;
            await SendPacket(new NpInfoPacket());
            await SendPacket(new PclearPacket());

            if (classType == CharacterClassType.Adventurer)
            {
                HairStyle = HairStyle > HairStyleType.HairStyleB ? 0 : HairStyle;
            }

            LoadSpeed();

            Class = classType;
            Hp = MaxHp;
            Mp = MaxMp;
            await SendPacket(GenerateTit());
            await SendPacket(GenerateStat());
            await MapInstance.SendPacket(GenerateEq());
            await MapInstance.SendPacket(this.GenerateEff(8));
            //TODO: Faction
            await SendPacket(this.GenerateCond());
            await SendPacket(GenerateLev());
            await SendPacket(this.GenerateCMode());
            await SendPacket(new MsgPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.CLASS_CHANGED, Account.Language),
                Type = MessageType.White
            });

            QuicklistEntries = new List<QuicklistEntryDto>
            {
                new QuicklistEntryDto
                {
                    CharacterId = CharacterId,
                    Q1 = 0,
                    Q2 = 9,
                    Type = QSetType.Set,
                    Slot = 3,
                    Pos = 1
                }
            };

            await MapInstance.SendPacket(this.GenerateIn(Prefix), new EveryoneBut(Session.Channel.Id));
            await MapInstance.SendPacket(Group.GeneratePidx(this));
            await MapInstance.SendPacket(this.GenerateEff(6));
            await MapInstance.SendPacket(this.GenerateEff(198));
        }

        public Task AddGold(long gold)
        {
            Gold += gold;
            return SendPacket(this.GenerateGold());
        }

        public Task RemoveGold(long gold)
        {
            Gold -= gold;
            return SendPacket(this.GenerateGold());
        }

        public void AddBankGold(long bankGold)
        {
            Account.BankMoney += bankGold;
        }

        public void RemoveBankGold(long bankGold)
        {
            Account.BankMoney -= bankGold;
        }

        public async Task SetGold(long gold)
        {
            Gold = gold;
            await SendPacket(this.GenerateGold());
            await SendPacket(this.GenerateSay(
                 Language.Instance.GetMessageFromKey(LanguageKey.UPDATE_GOLD, Session.Account.Language),
                 SayColorType.Purple));
        }

        public async Task SetReputation(long reput)
        {
            Reput = reput;
            await SendPacket(GenerateFd());
            await SendPacket(this.GenerateSay(
                Language.Instance.GetMessageFromKey(LanguageKey.REPUTATION_CHANGED, Session.Account.Language),
                SayColorType.Purple));
        }
        public async Task GenerateMail(IEnumerable<MailData> mails)
        {
            foreach (var mail in mails)
            {
                if (!mail.MailDto.IsSenderCopy && (mail.ReceiverName == Name))
                {
                    if (mail.ItemInstance != null)
                    {
                        await Session.SendPacket(mail.GeneratePost(0));
                    }
                    else
                    {
                        await Session.SendPacket(mail.GeneratePost(1));
                    }
                }
                else
                {
                    if (mail.ItemInstance != null)
                    {
                        await Session.SendPacket(mail.GeneratePost(3));
                    }
                    else
                    {
                        await Session.SendPacket(mail.GeneratePost(2));
                    }
                }
            }
        }

        public Task ChangeMap(short mapId, short mapX, short mapY)
        {
            return Session.ChangeMap(mapId, mapX, mapY);
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            return Session.GetMessageFromKey(languageKey);
        }

        public async Task CloseShop()
        {
            Shop = null;

            await MapInstance.SendPacket(this.GenerateShop());
            await MapInstance.SendPacket(this.GeneratePFlag());

            IsSitting = false;
            LoadSpeed();
            await SendPacket(this.GenerateCond());
            await MapInstance.SendPacket(this.GenerateRest());
        }

        public RsfiPacket GenerateRsfi()
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

        public Tuple<double, byte> GenerateShopRates()
        {
            byte shopKind = 100;
            var percent = 1.0;
            switch (GetDignityIco())
            {
                case 3:
                    percent = 1.1;
                    shopKind = 110;
                    break;

                case 4:
                    percent = 1.2;
                    shopKind = 120;
                    break;

                case 5:
                case 6:
                    percent = 1.5;
                    shopKind = 150;
                    break;
            }

            return new Tuple<double, byte>(percent, shopKind);
        }

        public async Task Buy(Shop shop, short slot, short amount)
        {
            var item = shop.ShopItems.Values.FirstOrDefault(it => it.Slot == slot);
            if (item == null)
            {
                return;
            }

            var price = item.Price ?? item.ItemInstance.Item.Price * amount;
            var reputprice = item.Price == null ? item.ItemInstance.Item.ReputPrice * amount : 0;
            var percent = GenerateShopRates().Item1;

            if (amount > item.Amount)
            {
                //todo LOG
                return;
            }

            if ((reputprice == 0) && (price * percent > Gold))
            {
                await SendPacket(new SMemoPacket
                {
                    Type = SMemoType.FatalError,
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, Account.Language)
                });
                return;
            }

            if (reputprice > Reput)
            {
                await SendPacket(new SMemoPacket
                {
                    Type = SMemoType.FatalError,
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_REPUT, Account.Language)
                });
                return;
            }

            short slotChar = item.Slot;
            List<InventoryItemInstance> inv;
            if (shop.Session == null)
            {
                inv = InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                    ItemProvider.Create(item.ItemInstance.ItemVNum, amount), Session.Character.CharacterId));
            }
            else
            {
                if (price + shop.Session.Character.Gold > shop.Session.WorldConfiguration.MaxGoldAmount)
                {
                    await SendPacket(new SMemoPacket
                    {
                        Type = SMemoType.FatalError,
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.TOO_RICH_SELLER, Account.Language)
                    });
                    return;
                }

                if (amount == item.ItemInstance.Amount)
                {
                    inv = InventoryService.AddItemToPocket(InventoryItemInstance.Create(item.ItemInstance,
                        Session.Character.CharacterId));
                }
                else
                {
                    inv = InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                        ItemProvider.Create(item.ItemInstance.ItemVNum, amount), Session.Character.CharacterId));
                }
            }

            if (inv?.Count > 0)
            {
                inv.ForEach(it => it.CharacterId = CharacterId);
                var packet = await (shop.Session == null ? Task.FromResult((NInvPacket?)null) : shop.Session.Character!.BuyFrom(item, amount, slotChar));
                if (packet != null)
                {
                    await SendPacket(packet);
                }

                await SendPackets(
                    inv.Select(invItem => invItem.GeneratePocketChange((PocketType)invItem.Type, invItem.Slot)));
                await SendPacket(new SMemoPacket
                {
                    Type = SMemoType.Success,
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BUY_ITEM_VALID, Account.Language)
                });
                if (reputprice == 0)
                {
                    Gold -= (long)(price * percent);
                    await SendPacket(this.GenerateGold());
                }
                else
                {
                    Reput -= reputprice;
                    await SendPacket(GenerateFd());
                    await SendPacket(this.GenerateSay(
                        Language.Instance.GetMessageFromKey(LanguageKey.REPUT_DECREASED, Account.Language),
                        SayColorType.Purple));
                }
            }
            else
            {
                await SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                        Session.Account.Language),
                    Type = 0
                });
            }
        }

        private async Task<NInvPacket?> BuyFrom(ShopItem item, short amount, short slotChar)
        {
            var type = item.Type;
            var itemInstance = amount == item.ItemInstance.Amount
                ? InventoryService.DeleteById(item.ItemInstance.Id)
                : InventoryService.RemoveItemAmountFromInventory(amount, item.ItemInstance.Id);
            var slot = item.Slot;
            item.Amount -= amount;
            if ((item?.Amount ?? 0) == 0)
            {
                Shop.ShopItems.TryRemove(slot, out _);
            }

            await SendPacket(itemInstance.GeneratePocketChange((PocketType)type, slotChar));
            await SendPacket(new SMemoPacket
            {
                Type = SMemoType.Success,
                Message = string.Format(
                    Language.Instance.GetMessageFromKey(LanguageKey.BUY_ITEM_FROM, Account.Language), Name,
                    item.ItemInstance.Item.Name[Account.Language], amount)
            });
            var sellAmount = (item?.Price ?? 0) * amount;
            Gold += sellAmount;
            await SendPacket(this.GenerateGold());
            Shop.Sell += sellAmount;

            await SendPacket(new SellListPacket
            {
                ValueSold = Shop.Sell,
                SellListSubPacket = new List<SellListSubPacket?>
                {
                    new SellListSubPacket
                    {
                        Amount = item?.Amount ?? 0,
                        Slot = slot,
                        SellAmount = item?.Amount ?? 0
                    }
                }
            });

            if (Shop.ShopItems.Count == 0)
            {
                await CloseShop();
                return null;
            }

            return this.GenerateNInv(1, 0, 0);
        }

        private async Task GenerateLevelupPackets()
        {
            await SendPacket(GenerateStat());
            await SendPacket(this.GenerateStatInfo());
            //Session.SendPacket(GenerateStatChar());
            await SendPacket(GenerateLev());
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);

            Parallel.ForEach(mapSessions, s =>
            {
                if (s.VisualId != VisualId)
                {
                    s.SendPacket(this.GenerateIn(Authority == AuthorityType.Moderator
                        ? Language.Instance.GetMessageFromKey(LanguageKey.SUPPORT, Account.Language) : string.Empty));
                    //TODO: Generate GIDX
                }

                s.SendPacket(this.GenerateEff(6));
                s.SendPacket(this.GenerateEff(198));
            });

            foreach (var member in Group.Keys)
            {
                var groupMember = Broadcaster.Instance.GetCharacter(s =>
                    (s.VisualId == member.Item2) && (member.Item1 == VisualType.Player));

                groupMember?.SendPacket(groupMember.Group.GeneratePinit());
            }

            await SendPacket(Group.GeneratePinit());
        }

        public async Task SetLevel(byte level)
        {
            (this as INamedEntity).SetLevel(level);
            await GenerateLevelupPackets();
            await SendPacket(new MsgPacket
            {
                Type = MessageType.White,
                Message = Language.Instance.GetMessageFromKey(LanguageKey.LEVEL_CHANGED, Session.Account.Language)
            });
        }

        public LevPacket GenerateLev()
        {
            return new LevPacket
            {
                Level = Level,
                LevelXp = LevelXp,
                JobLevel = JobLevel,
                JobLevelXp = JobLevelXp,
                XpLoad = (int)CharacterHelper.Instance.XpLoad(Level),
                JobXpLoad = (int)CharacterHelper.Instance.JobXpLoad(JobLevel, Class),
                Reputation = Reput,
                SkillCp = 0,
                HeroXp = HeroXp,
                HeroLevel = HeroLevel,
                HeroXpLoad = (int)CharacterHelper.Instance.HeroXpLoad(HeroLevel)
            };
        }

        public IEnumerable<QSlotPacket> GenerateQuicklist()
        {
            var pktQs = new QSlotPacket[2];
            for (var i = 0; i < pktQs.Length; i++)
            {
                var subpacket = new List<QsetClientSubPacket>();
                for (var j = 0; j < 30; j++)
                {
                    var qi = QuicklistEntries.FirstOrDefault(n =>
                        (n.Q1 == i) && (n.Q2 == j) && (n.Morph == (UseSp ? Morph : 0)));

                    subpacket.Add(new QsetClientSubPacket
                    {
                        Type = qi?.Type ?? QSetType.Reset,
                        OriginQuickListSlot = qi?.Slot ?? 7,
                        Data = qi?.Pos ?? -1
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

        public FdPacket GenerateFd()
        {
            return new FdPacket
            {
                Reput = Reput,
                Dignity = (int)Dignity,
                ReputIcon = GetReputIco(),
                DignityIcon = Math.Abs(GetDignityIco())
            };
        }

        public int GetDignityIco()
        {
            var icoDignity = 1;

            if (Dignity <= -100)
            {
                icoDignity = 2;
            }

            if (Dignity <= -200)
            {
                icoDignity = 3;
            }

            if (Dignity <= -400)
            {
                icoDignity = 4;
            }

            if (Dignity <= -600)
            {
                icoDignity = 5;
            }

            if (Dignity <= -800)
            {
                icoDignity = 6;
            }

            return icoDignity;
        }

        public int IsReputHero()
        {
            //const int i = 0;
            //foreach (CharacterDTO characterDto in Broadcaster.Instance.TopReputation)
            //{
            //    Character character = (Character)characterDto;
            //    i++;
            //    if (character.CharacterId != CharacterId)
            //    {
            //        continue;
            //    }
            //    switch (i)
            //    {
            //        case 1:
            //            return 5;
            //        case 2:
            //            return 4;
            //        case 3:
            //            return 3;
            //    }
            //    if (i <= 13)
            //    {
            //        return 2;
            //    }
            //    if (i <= 43)
            //    {
            //        return 1;
            //    }
            //}
            return 0;
        }

        public SpPacket GenerateSpPoint()
        {
            return new SpPacket
            {
                AdditionalPoint = SpAdditionPoint,
                MaxAdditionalPoint = Session.WorldConfiguration.MaxAdditionalSpPoints,
                SpPoint = SpPoint,
                MaxSpPoint = Session.WorldConfiguration.MaxSpPoints
            };
        }

        [Obsolete(
            "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
        public IEnumerable<IPacket> GenerateInv()
        {
            var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket>() };
            var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket>() };
            var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket>() };
            var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket>() };
            var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket>() };
            var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket>() };

            if (InventoryService != null)
            {
                foreach (var inv in InventoryService.Select(s => s.Value))
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
                            _logger.Information(
                                LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.POCKETTYPE_UNKNOWN));
                            break;
                    }
                }
            }

            return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7 };
        }

        public int GetReputIco()
        {
            if (Reput <= 50)
            {
                return 1;
            }

            if (Reput <= 150)
            {
                return 2;
            }

            if (Reput <= 250)
            {
                return 3;
            }

            if (Reput <= 500)
            {
                return 4;
            }

            if (Reput <= 750)
            {
                return 5;
            }

            if (Reput <= 1000)
            {
                return 6;
            }

            if (Reput <= 2250)
            {
                return 7;
            }

            if (Reput <= 3500)
            {
                return 8;
            }

            if (Reput <= 5000)
            {
                return 9;
            }

            if (Reput <= 9500)
            {
                return 10;
            }

            if (Reput <= 19000)
            {
                return 11;
            }

            if (Reput <= 25000)
            {
                return 12;
            }

            if (Reput <= 40000)
            {
                return 13;
            }

            if (Reput <= 60000)
            {
                return 14;
            }

            if (Reput <= 85000)
            {
                return 15;
            }

            if (Reput <= 115000)
            {
                return 16;
            }

            if (Reput <= 150000)
            {
                return 17;
            }

            if (Reput <= 190000)
            {
                return 18;
            }

            if (Reput <= 235000)
            {
                return 19;
            }

            if (Reput <= 285000)
            {
                return 20;
            }

            if (Reput <= 350000)
            {
                return 21;
            }

            if (Reput <= 500000)
            {
                return 22;
            }

            if (Reput <= 1500000)
            {
                return 23;
            }

            if (Reput <= 2500000)
            {
                return 24;
            }

            if (Reput <= 3750000)
            {
                return 25;
            }

            if (Reput <= 5000000)
            {
                return 26;
            }

            if (Reput >= 5000001)
            {
                return (IsReputHero()) switch
                {
                    1 => 28,
                    2 => 29,
                    3 => 30,
                    4 => 31,
                    5 => 32,
                    _ => 27,
                };
            }

            return 0;
        }

        public void LoadSpeed()
        {
            Speed = CharacterHelper.Instance.SpeedData[(byte)Class];
        }

        public double MpLoad()
        {
            const int mp = 0;
            const double multiplicator = 1.0;
            return (int)((CharacterHelper.Instance.MpData[(byte)Class][Level] + mp) * multiplicator);
        }

        public double HpLoad()
        {
            const double multiplicator = 1.0;
            const int hp = 0;

            return (int)((CharacterHelper.Instance.HpData[(byte)Class][Level] + hp) * multiplicator);
        }

        public AtPacket GenerateAt()
        {
            return new AtPacket
            {
                CharacterId = CharacterId,
                MapId = MapInstance.Map.MapId,
                PositionX = PositionX,
                PositionY = PositionY,
                Direction = Direction,
                Unknown1 = 0,
                Music = MapInstance.Map.Music,
                Unknown2 = 0,
                Unknown3 = -1
            };
        }

        public TitPacket GenerateTit()
        {
            return new TitPacket
            {
                ClassType = Session.GetMessageFromKey((LanguageKey)Enum.Parse(typeof(LanguageKey),
                    Enum.Parse(typeof(CharacterClassType), Class.ToString()).ToString().ToUpperInvariant())),
                Name = Name
            };
        }

        public CInfoPacket GenerateCInfo()
        {
            return new CInfoPacket
            {
                Name = Account.Authority == AuthorityType.Moderator
                    ? $"[{Session.GetMessageFromKey(LanguageKey.SUPPORT)}]" + Name : Name,
                Unknown1 = null,
                GroupId = -1,
                FamilyId = -1,
                FamilyName = null,
                CharacterId = CharacterId,
                Authority = (AuthorityUIType)(int)Account.Authority,
                Gender = Gender,
                HairStyle = HairStyle,
                HairColor = HairColor,
                Class = Class,
                Icon = (byte)(GetDignityIco() == 1 ? GetReputIco() : -GetDignityIco()),
                Compliment = (short)(Account.Authority == AuthorityType.Moderator ? 500 : Compliment),
                Morph = 0,
                Invisible = false,
                FamilyLevel = 0,
                MorphUpgrade = 0,
                ArenaWinner = false
            };
        }

        public StatPacket GenerateStat()
        {
            return new StatPacket
            {
                Hp = Hp,
                HpMaximum = HpLoad(),
                Mp = Mp,
                MpMaximum = MpLoad(),
                Unknown = 0,
                Option = 0
            };
        }

        public TalkPacket GenerateTalk(string message)
        {
            return new TalkPacket
            {
                CharacterId = CharacterId,
                Message = message
            };
        }

        public void AddSpPoints(int spPointToAdd)
        {
            SpPoint = SpPoint + spPointToAdd > Session.WorldConfiguration.MaxSpPoints
                ? Session.WorldConfiguration.MaxSpPoints : SpPoint + spPointToAdd;
            SendPacket(GenerateSpPoint());
        }

        public void AddAdditionalSpPoints(int spPointToAdd)
        {
            SpAdditionPoint = SpAdditionPoint + spPointToAdd > Session.WorldConfiguration.MaxAdditionalSpPoints
                ? Session.WorldConfiguration.MaxAdditionalSpPoints : SpAdditionPoint + spPointToAdd;
            SendPacket(GenerateSpPoint());
        }

        public EquipPacket GenerateEquipment()
        {
            EquipmentSubPacket GenerateEquipmentSubPacket(EquipmentType eqType)
            {
                var eq = InventoryService.LoadBySlotAndType((short)eqType, NoscorePocketType.Wear);
                if (eq == null)
                {
                    return null;
                }

                return new EquipmentSubPacket
                {
                    EquipmentType = eqType,
                    VNum = eq.ItemInstance.ItemVNum,
                    Rare = eq.ItemInstance.Rare,
                    Upgrade = (eq?.ItemInstance.Item.IsColored == true ? eq.ItemInstance?.Design
                        : eq?.ItemInstance.Upgrade) ?? 0,
                    Unknown = 0
                };
            }

            return new EquipPacket
            {
                WeaponUpgradeRareSubPacket = WeaponUpgradeRareSubPacket,
                ArmorUpgradeRareSubPacket = ArmorUpgradeRareSubPacket,
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

        public EqPacket GenerateEq()
        {
            return new EqPacket
            {
                VisualId = VisualId,
                Visibility = (byte)(Authority < AuthorityType.GameMaster ? 0 : 2),
                Gender = Gender,
                HairStyle = HairStyle,
                Haircolor = HairColor,
                ClassType = Class,
                EqSubPacket = Equipment,
                WeaponUpgradeRarePacket = WeaponUpgradeRareSubPacket,
                ArmorUpgradeRarePacket = ArmorUpgradeRareSubPacket
            };
        }

        public async Task RemoveSp()
        {
            UseSp = false;
            Morph = 0;
            MorphUpgrade = 0;
            MorphDesign = 0;
            LoadSpeed();
            await SendPacket(this.GenerateCond());
            await SendPacket(GenerateLev());
            SpCooldown = 30;
            await SendPacket(this.GenerateSay(
                string.Format(Language.Instance.GetMessageFromKey(LanguageKey.STAY_TIME, Account.Language), SpCooldown),
                SayColorType.Purple));
            await SendPacket(new SdPacket { Cooldown = SpCooldown });
            await MapInstance.SendPacket(this.GenerateCMode());
            await MapInstance.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.Unknow2,
                Value = 1,
                EntityId = CharacterId
            });
            await SendPacket(GenerateStat());

            Observable.Timer(TimeSpan.FromMilliseconds(SpCooldown * 1000)).Subscribe(o =>
            {
                SendPacket(this.GenerateSay(
                   string.Format(
                       Language.Instance.GetMessageFromKey(LanguageKey.TRANSFORM_DISAPPEAR, Account.Language),
                       SpCooldown), SayColorType.Purple));
                SendPacket(new SdPacket { Cooldown = 0 });
            });
        }

        public async Task ChangeSp()
        {
            if (!(InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is
                SpecialistInstance sp))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.USE_SP_WITHOUT_SP_ERROR));
                return;
            }

            if (GetReputIco() < sp.Item.ReputationMinimum)
            {
                await SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.LOW_REP,
                        Session.Account.Language)
                });
                return;
            }

            if (InventoryService.LoadBySlotAndType((byte)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance is
                    WearableInstance fairy
                && (sp.Item.Element != 0) && (fairy.Item.Element != sp.Item.Element)
                && (fairy.Item.Element != sp.Item.SecondaryElement))
            {
                await SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY,
                        Session.Account.Language)
                });
                return;
            }

            LastSp = SystemTime.Now();
            UseSp = true;
            Morph = sp.Item.Morph;
            MorphUpgrade = sp.Upgrade;
            MorphDesign = sp.Design;
            await MapInstance.SendPacket(this.GenerateCMode());
            await SendPacket(GenerateLev());
            await MapInstance.SendPacket(this.GenerateEff(196));
            await MapInstance.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.Unknow2,
                Value = 1,
                EntityId = CharacterId
            });
            await SendPacket(GenerateSpPoint());
            LoadSpeed();
            await SendPacket(this.GenerateCond());
            await SendPacket(GenerateStat());
        }

        public async Task RemoveVehicle()
        {
            if (UseSp)
            {
                var sp =
                    InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear);
                if (sp != null)
                {
                    Morph = sp.ItemInstance.Item.Morph;
                    MorphDesign = sp.ItemInstance.Design;
                    MorphUpgrade = sp.ItemInstance.Upgrade;
                }
                else
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.USE_SP_WITHOUT_SP_ERROR));
                }
            }
            else
            {
                Morph = 0;
            }

            IsVehicled = false;
            VehicleSpeed = 0;
            await SendPacket(this.GenerateCond());
            await MapInstance.SendPacket(this.GenerateCMode());
        }

        public MlobjlstPacket GenerateMlobjlst()
        {
            var mlobj = new List<MlobjlstSubPacket>();
            foreach (var item in InventoryService.Where(s => s.Value.Type == NoscorePocketType.Miniland)
                .OrderBy(s => s.Value.Slot).Select(s => s.Value))
            {
                var used = Session.Character.MapInstance.MapDesignObjects.ContainsKey(item.Id);
                var mp = used ? Session.Character.MapInstance.MapDesignObjects[item.Id] : null;

                mlobj.Add(new MlobjlstSubPacket
                {
                    InUse = used,
                    Slot = item.Slot,
                    MlObjSubPacket = new MlobjSubPacket
                    {
                        MapX = used ? mp.MapX : (short)0,
                        MapY = used ? mp.MapY : (short)0,
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
    }
}