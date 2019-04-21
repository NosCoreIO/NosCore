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

using DotNetty.Transport.Channels;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using SpecialistInstance = NosCore.GameObject.Providers.ItemProvider.Item.SpecialistInstance;
using WearableInstance = NosCore.GameObject.Providers.ItemProvider.Item.WearableInstance;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Player;
using ChickenAPI.Packets.ServerPackets.Chats;
using ChickenAPI.Packets.ServerPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.Specialists;
using ChickenAPI.Packets.ClientPackets.UI;
using ChickenAPI.Packets.ServerPackets.UI;
using ChickenAPI.Packets.ServerPackets.Shop;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.ServerPackets.Visibility;
using ChickenAPI.Packets.ServerPackets.Relations;
using NosCore.Data.Enumerations.Interaction;
using ChickenAPI.Packets.ServerPackets.Minimap;

namespace NosCore.GameObject
{
    public class Character : CharacterDto, ICharacterEntity
    {
        private readonly ILogger _logger;
        private readonly ISerializer _packetSerializer;
        private byte _speed;
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IGenericDao<AccountDto> _accountDao;

        public Character(IInventoryService inventory, IExchangeProvider exchangeProvider, IItemProvider itemProvider, IGenericDao<CharacterRelationDto> characterRelationDao
            ,IGenericDao<CharacterDto> characterDao, IGenericDao<IItemInstanceDto> itemInstanceDao, IGenericDao<AccountDto> accountDao, ILogger logger, ISerializer packetSerializer)
        {
            Inventory = inventory;
            ExchangeProvider = exchangeProvider;
            ItemProvider = itemProvider;
            FriendRequestCharacters = new ConcurrentDictionary<long, long>();
            CharacterRelations = new ConcurrentDictionary<Guid, CharacterRelation>();
            RelationWithCharacter = new ConcurrentDictionary<Guid, CharacterRelation>();
            GroupRequestCharacterIds = new ConcurrentDictionary<long, long>();
            Group = new Group(GroupType.Group);
            Requests = new Subject<RequestData>();
            _characterRelationDao = characterRelationDao;
            _characterDao = characterDao;
            _itemInstanceDao = itemInstanceDao;
            _accountDao = accountDao;
            _logger = logger;
            _packetSerializer = packetSerializer;
        }

        public AccountDto Account { get; set; }

        public bool IsChangingMapInstance { get; set; }

        public bool IsFriendListFull
        {
            get => CharacterRelations.Where(s => s.Value.RelationType == CharacterRelationType.Friend).ToList().Count
                >= 80;
        }

        public DateTime LastPortal { get; set; }

        public ClientSession Session { get; set; }

        public DateTime LastSpeedChange { get; set; }

        public DateTime LastMove { get; set; }
        public IItemProvider ItemProvider { get; set; }
        public bool InExchangeOrTrade { get; set; }

        public bool UseSp { get; set; }

        public DateTime LastSp { get; set; } = SystemTime.Now();
        public short SpCooldown { get; set; }
        public bool IsVehicled { get; set; }
        public byte? VehicleSpeed { get; set; }

        public IExchangeProvider ExchangeProvider { get; }

        public bool InExchangeOrShop => InExchange || InShop;

        public bool InExchange => ExchangeProvider.CheckExchange(VisualId);

        public bool InShop { get; set; }

        public long BankGold => Account.BankMoney;

        public RegionType AccountLanguage => Account.Language;

        public ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; set; }

        public ConcurrentDictionary<Guid, CharacterRelation> CharacterRelations { get; set; }

        public ConcurrentDictionary<Guid, CharacterRelation> RelationWithCharacter { get; set; }
        public Subject<RequestData> Requests { get; set; }
        public ConcurrentDictionary<long, long> FriendRequestCharacters { get; set; }

        public short Race => (byte) Class;
        public Shop Shop { get; set; }

        public bool Camouflage { get; set; }

        public bool Invisible { get; set; }

        public IInventoryService Inventory { get; }

        public Group Group { get; set; }

        public int ReputIcon => GetReputIco();

        public int DignityIcon => GetDignityIco();

        public IChannel Channel => Session?.Channel;

        public void SendPacket(IPacket packetDefinition) => Session.SendPacket(packetDefinition);

        public void SendPackets(IEnumerable<IPacket> packetDefinitions) =>
            Session.SendPackets(packetDefinitions);

        public MapInstance MapInstance { get; set; }

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
                    return (byte) VehicleSpeed;
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

                return (byte) (_speed + bonusSpeed);
            }

            set
            {
                LastSpeedChange = SystemTime.Now();
                _speed = value > 59 ? (byte) 59 : value;
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

        public int MaxHp => (int) HpLoad();

        public int MaxMp => (int) MpLoad();

        public void SetHeroLevel(byte level)
        {
            HeroLevel = level;
            HeroXp = 0;
            GenerateLevelupPackets();
            SendPacket(new MsgPacket
            {
                Type = MessageType.White,
                Message = Language.Instance.GetMessageFromKey(LanguageKey.HERO_LEVEL_CHANGED, Session.Account.Language)
            });
        }

        public void SetJobLevel(byte jobLevel)
        {
            JobLevel = (byte) ((CharacterClassType) Class == CharacterClassType.Adventurer && jobLevel > 20 ? 20
                : jobLevel);
            JobLevelXp = 0;
            SendPacket(GenerateLev());
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);
            Parallel.ForEach(mapSessions, s =>
            {
                //if (s.VisualId != VisualId)
                //{
                //    TODO: Generate GIDX
                //}

                s.SendPacket(this.GenerateEff(8));
            });
            SendPacket(new MsgPacket
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

        public void LeaveGroup()
        {
            Group.LeaveGroup(this);
            foreach (var member in Group.Keys.Where(s => s.Item2 != CharacterId || s.Item1 != VisualType.Player))
            {
                var groupMember = Broadcaster.Instance.GetCharacter(s =>
                    s.VisualId == member.Item2 && member.Item1 == VisualType.Player);

                if (Group.Count == 1)
                {
                    groupMember?.LeaveGroup();
                    groupMember?.SendPacket(Group.GeneratePidx(groupMember));
                    groupMember?.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_CLOSED, groupMember.AccountLanguage),
                        Type = MessageType.White
                    });
                }

                groupMember?.SendPacket(groupMember.Group.GeneratePinit());
            }

            Group = new Group(GroupType.Group);
            Group.JoinGroup(this);
        }

        public string GetMessageFromKey(LanguageKey languageKey) => Session.GetMessageFromKey(languageKey);

        public CharacterRelation AddRelation(long characterId, CharacterRelationType relationType)
        {
            var relation = new CharacterRelation
            {
                CharacterId = CharacterId,
                RelatedCharacterId = characterId,
                RelationType = relationType,
                CharacterName = Broadcaster.Instance.GetCharacter(s => s.VisualId == characterId)?.Name,
                CharacterRelationId = Guid.NewGuid()
            };

            CharacterRelations[relation.CharacterRelationId] = relation;
            CharacterRelationDto relationDto = relation;

            if (_characterRelationDao.FirstOrDefault(s =>
                s.CharacterId == CharacterId && s.RelatedCharacterId == characterId) == null)
            {
                _characterRelationDao.InsertOrUpdate(ref relationDto);
            }

            if (relationType == CharacterRelationType.Blocked)
            {
                SendPacket(GenerateBlinit());
                return relation;
            }

            SendPacket(this.GenerateFinit());
            return relation;
        }

        public void Save()
        {
            try
            {
                var account = Session.Account;
                _accountDao.InsertOrUpdate(ref account);

                CharacterDto character = (Character) MemberwiseClone();
               _characterDao.InsertOrUpdate(ref character);

                var savedRelations = _characterRelationDao.Where(s => s.CharacterId == CharacterId);
                _characterRelationDao.Delete(savedRelations.Except(CharacterRelations.Values));
                _characterRelationDao.InsertOrUpdate(CharacterRelations.Values);

                // load and concat inventory with equipment
                var currentlySavedInventorys = _itemInstanceDao
                    .Where(i => i.CharacterId == CharacterId);
                _itemInstanceDao
                    .Delete(currentlySavedInventorys.Except(Inventory.Values.ToArray()));
                _itemInstanceDao.InsertOrUpdate(Inventory.Values.ToArray());
            }
            catch (Exception e)
            {
                _logger.Error("Save Character failed. SessionId: " + Session.SessionId, e);
            }
        }

        public InEquipmentSubPacket Equipment => new InEquipmentSubPacket
        {
            Armor = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.Armor, PocketType.Wear)?.ItemVNum,
            CostumeHat = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.CostumeHat, PocketType.Wear)
                ?.ItemVNum,
            CostumeSuit = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.CostumeSuit, PocketType.Wear)
                ?.ItemVNum,
            Fairy = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.Fairy, PocketType.Wear)?.ItemVNum,
            Hat = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.Hat, PocketType.Wear)?.ItemVNum,
            MainWeapon = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.MainWeapon, PocketType.Wear)
                ?.ItemVNum,
            Mask = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.Mask, PocketType.Wear)?.ItemVNum,
            SecondaryWeapon = Inventory
                .LoadBySlotAndType<IItemInstance>((short) EquipmentType.SecondaryWeapon, PocketType.Wear)?.ItemVNum,
            WeaponSkin = Inventory.LoadBySlotAndType<IItemInstance>((short) EquipmentType.WeaponSkin, PocketType.Wear)
                ?.ItemVNum
        };

        public UpgradeRareSubPacket WeaponUpgradeRareSubPacket
        {
            get
            {
                var weapon =
                    Inventory.LoadBySlotAndType<WearableInstance>((short) EquipmentType.MainWeapon, PocketType.Wear);
                return new UpgradeRareSubPacket
                {
                    Upgrade = weapon?.Upgrade ?? 0,
                    Rare = (byte) (weapon?.Rare ?? 0)
                };
            }
        }

        public UpgradeRareSubPacket ArmorUpgradeRareSubPacket
        {
            get
            {
                var armor = Inventory.LoadBySlotAndType<WearableInstance>((short) EquipmentType.Armor, PocketType.Wear);
                return new UpgradeRareSubPacket
                {
                    Upgrade = armor?.Upgrade ?? 0,
                    Rare = (byte) (armor?.Rare ?? 0)
                };
            }
        }

        public void ChangeClass(CharacterClassType classType)
        {
            if (Class == classType)
            {
                _logger.Error(
                    Language.Instance.GetMessageFromKey(LanguageKey.CANT_CHANGE_SAME_CLASS, Account.Language));
                return;
            }

            JobLevel = 1;
            JobLevelXp = 0;
            SendPacket(new NpInfoPacket());
            SendPacket(new PclearPacket());

            if (classType == CharacterClassType.Adventurer)
            {
                HairStyle = HairStyle > HairStyleType.HairStyleB ? 0 : HairStyle;
            }

            LoadSpeed();

            Class = classType;
            Hp = MaxHp;
            Mp = MaxMp;
            SendPacket(GenerateTit());
            SendPacket(GenerateStat());
            MapInstance.Sessions.SendPacket(GenerateEq());
            MapInstance.Sessions.SendPacket(this.GenerateEff(8));
            //TODO: Faction
            SendPacket(this.GenerateCond());
            SendPacket(GenerateLev());
            SendPacket(this.GenerateCMode());
            SendPacket(new MsgPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.CLASS_CHANGED, Account.Language),
                Type = MessageType.White
            });
            MapInstance.Sessions.SendPacket(this.GenerateIn(Prefix), new EveryoneBut(Session.Channel.Id));

            MapInstance.Sessions.SendPacket(Group.GeneratePidx(this));
            MapInstance.Sessions.SendPacket(this.GenerateEff(6));
            MapInstance.Sessions.SendPacket(this.GenerateEff(198));
        }

        public void AddGold(long gold)
        {
            Gold += gold;
            SendPacket(this.GenerateGold());
        }

        public void RemoveGold(long gold)
        {
            Gold -= gold;
            SendPacket(this.GenerateGold());
        }

        public void AddBankGold(long bankGold)
        {
            Account.BankMoney += bankGold;
        }

        public void RemoveBankGold(long bankGold)
        {
            Account.BankMoney -= bankGold;
        }

        public void SetGold(long gold)
        {
            Gold = gold;
            SendPacket(this.GenerateGold());
            SendPacket(this.GenerateSay(
                Language.Instance.GetMessageFromKey(LanguageKey.UPDATE_GOLD, Session.Account.Language),
                SayColorType.Purple));
        }

        public void SetReputation(long reput)
        {
            Reput = reput;
            SendPacket(GenerateFd());
            SendPacket(this.GenerateSay(
                Language.Instance.GetMessageFromKey(LanguageKey.REPUTATION_CHANGED, Session.Account.Language),
                SayColorType.Purple));
        }

        public void CloseShop()
        {
            Shop = null;

            MapInstance.Sessions.SendPacket(this.GenerateShop());
            MapInstance.Sessions.SendPacket(this.GeneratePFlag());

            IsSitting = false;
            LoadSpeed();
            SendPacket(this.GenerateCond());
            MapInstance.Sessions.SendPacket(this.GenerateRest());
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

                default:
                    break;
            }

            return new Tuple<double, byte>(percent, shopKind);
        }

        public void Buy(Shop shop, short slot, short amount)
        {
            var item = shop.ShopItems.Values.FirstOrDefault(it => it.Slot == slot);
            if (item == null)
            {
                return;
            }

            var price = item.Price ?? item.ItemInstance.Item.Price * amount;
            var reputprice = item.Price == null ? item.ItemInstance.Item.ReputPrice * amount : 0;
            double percent = GenerateShopRates().Item1;

            if (amount > item.Amount)
            {
                //todo LOG
                return;
            }

            if (reputprice == 0 && price * percent > Gold)
            {
                SendPacket(new SMemoPacket
                {
                    Type = SMemoType.FatalError,
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, Account.Language)
                });
                return;
            }

            if (reputprice > Reput)
            {
                SendPacket(new SMemoPacket
                {
                    Type = SMemoType.FatalError,
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_REPUT, Account.Language)
                });
                return;
            }

            short slotChar = item.ItemInstance.Slot;
            List<IItemInstance> inv;
            if (shop.Session == null)
            {
                inv = Inventory.AddItemToPocket(ItemProvider.Create(item.ItemInstance.ItemVNum, CharacterId,
                    amount));
            }
            else
            {
                if (price + shop.Session.Character.Gold > shop.Session.WorldConfiguration.MaxGoldAmount)
                {
                    SendPacket(new SMemoPacket
                    {
                        Type = SMemoType.FatalError,
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.TOO_RICH_SELLER, Account.Language)
                    });
                    return;
                }

                if (amount == item.ItemInstance.Amount)
                {
                    inv = Inventory.AddItemToPocket(item.ItemInstance);
                }
                else
                {
                    inv = Inventory.AddItemToPocket(
                        ItemProvider.Create(item.ItemInstance.ItemVNum, CharacterId, amount));
                }
            }

            if (inv?.Count > 0)
            {
                inv.ForEach(it => it.CharacterId = CharacterId);
                var packet = shop.Session?.Character.BuyFrom(item, amount, slotChar);
                if (packet != null)
                {
                    SendPacket(packet);
                }

                SendPackets(inv.Select(invItem => invItem.GeneratePocketChange(invItem.Type, invItem.Slot)));
                SendPacket(new SMemoPacket
                {
                    Type = SMemoType.Success,
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BUY_ITEM_VALID, Account.Language)
                });
                if (reputprice == 0)
                {
                    Gold -= (long) (price * percent);
                    SendPacket(this.GenerateGold());
                }
                else
                {
                    Reput -= reputprice;
                    SendPacket(GenerateFd());
                    SendPacket(this.GenerateSay(
                        Language.Instance.GetMessageFromKey(LanguageKey.REPUT_DECREASED, Account.Language),
                        SayColorType.Purple));
                }
            }
            else
            {
                SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                        Session.Account.Language),
                    Type = 0
                });
            }
        }

        private NInvPacket BuyFrom(ShopItem item, short amount, short slotChar)
        {
            var type = item.ItemInstance.Type;
            var itemInstance = amount == item.ItemInstance.Amount
                ? Inventory.DeleteById(item.ItemInstance.Id)
                : Inventory.RemoveItemAmountFromInventory(amount, item.ItemInstance.Id);
            var slot = item.Slot;
            item.Amount -= amount;
            if ((item?.Amount ?? 0) == 0)
            {
                Shop.ShopItems.TryRemove(slot, out _);
            }

            SendPacket(itemInstance.GeneratePocketChange(type, slotChar));
            SendPacket(new SMemoPacket
            {
                Type = SMemoType.Success,
                Message = string.Format(
                    Language.Instance.GetMessageFromKey(LanguageKey.BUY_ITEM_FROM, Account.Language), Name,
                    item.ItemInstance.Item.Name, amount)
            });
            var sellAmount = (item?.Price ?? 0) * amount;
            Gold += sellAmount;
            SendPacket(this.GenerateGold());
            Shop.Sell += sellAmount;

            SendPacket(new SellListPacket
            {
                ValueSold = Shop.Sell,
                SellListSubPacket = new List<SellListSubPacket>
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
                CloseShop();
                return null;
            }

            return this.GenerateNInv(1, 0, 0);
        }

        private void GenerateLevelupPackets()
        {
            SendPacket(GenerateStat());
            SendPacket(this.GenerateStatInfo());
            //Session.SendPacket(GenerateStatChar());
            SendPacket(GenerateLev());
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
                    s.VisualId == member.Item2 && member.Item1 == VisualType.Player);

                groupMember?.SendPacket(groupMember.Group.GeneratePinit());
            }

            SendPacket(Group.GeneratePinit());
        }

        public void SetLevel(byte level)
        {
            (this as INamedEntity).SetLevel(level);
            GenerateLevelupPackets();
            SendPacket(new MsgPacket
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
                XpLoad = (int) CharacterHelper.Instance.XpLoad(Level),
                JobXpLoad = (int) CharacterHelper.Instance.JobXpLoad(JobLevel, Class),
                Reputation = Reput,
                SkillCp = 0,
                HeroXp = HeroXp,
                HeroLevel = HeroLevel,
                HeroXpLoad = (int) CharacterHelper.Instance.HeroXpLoad(HeroLevel)
            };
        }

        public FdPacket GenerateFd()
        {
            return new FdPacket
            {
                Reput = Reput,
                Dignity = (int) Dignity,
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

        public void SendRelationStatus(bool status)
        {
            foreach (var characterRelation in CharacterRelations)
            {
                var targetSession =
                    Broadcaster.Instance.GetCharacter(s => s.VisualId == characterRelation.Value.RelatedCharacterId);

                if (targetSession != null)
                {
                    targetSession.SendPacket(new FinfoPacket
                    {
                        FriendList = new List<FinfoSubPackets>
                        {
                            new FinfoSubPackets
                            {
                                CharacterId = CharacterId,
                                IsConnected = status
                            }
                        }
                    });
                }
                else
                {
                    WebApiAccess.Instance.BroadcastPacket(new PostedPacket
                    {
                        Packet = _packetSerializer.Serialize(new[]
                        {
                            new FinfoPacket
                            {
                                FriendList = new List<FinfoSubPackets>
                                {
                                    new FinfoSubPackets
                                    {
                                        CharacterId = CharacterId,
                                        IsConnected = status
                                    }
                                }
                            }
                        }),
                        ReceiverType = ReceiverType.OnlySomeone,
                        SenderCharacter = new Data.WebApi.Character { Id = CharacterId, Name = Name },
                        ReceiverCharacter = new Data.WebApi.Character
                        {
                            Id = characterRelation.Value.RelatedCharacterId,
                            Name = characterRelation.Value.CharacterName
                        }
                    });
                }
            }
        }

        public BlinitPacket GenerateBlinit()
        {
            var subpackets = new List<BlinitSubPacket>();
            foreach (var relation in CharacterRelations.Values.Where(s =>
                s.RelationType == CharacterRelationType.Blocked))
            {
                if (relation.RelatedCharacterId == CharacterId)
                {
                    continue;
                }

                subpackets.Add(new BlinitSubPacket
                {
                    RelatedCharacterId = relation.RelatedCharacterId,
                    CharacterName = relation.CharacterName
                });
            }

            return new BlinitPacket {SubPackets = subpackets};
        }

        public void DeleteBlackList(long characterId)
        {
            var relation = CharacterRelations.Values.FirstOrDefault(s =>
                s.RelatedCharacterId == characterId && s.RelationType == CharacterRelationType.Blocked);

            if (relation == null)
            {
                SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        Session.Account.Language)
                });
                return;
            }

            CharacterRelations.TryRemove(relation.CharacterRelationId, out _);
            SendPacket(GenerateBlinit());
        }

        public void DeleteRelation(long relatedCharacterId)
        {
            var characterRelation =
                CharacterRelations.Values.FirstOrDefault(s => s.RelatedCharacterId == relatedCharacterId);
            var targetCharacterRelation =
                RelationWithCharacter.Values.FirstOrDefault(s => s.RelatedCharacterId == CharacterId);

            if (characterRelation == null || targetCharacterRelation == null)
            {
                return;
            }

            CharacterRelations.TryRemove(characterRelation.CharacterRelationId, out _);
            RelationWithCharacter.TryRemove(targetCharacterRelation.CharacterRelationId, out _);
            SendPacket(this.GenerateFinit());

            var targetSession = Broadcaster.Instance.GetCharacter(s =>
                s.VisualId == targetCharacterRelation.CharacterId);
            if (targetSession != null)
            {
                targetSession.CharacterRelations.TryRemove(targetCharacterRelation.CharacterRelationId,
                    out _);
                targetSession.RelationWithCharacter.TryRemove(characterRelation.CharacterRelationId, out _);
                targetSession.SendPacket(targetSession.GenerateFinit());
                return;
            }

            var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.Where(c => c.Type == ServerType.WorldServer);
            foreach (var server in servers ?? new List<ChannelInfo>())
            {
                var account = WebApiAccess.Instance
                    .Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, server.WebApi)
                    .Find(s => s.ConnectedCharacter.Id == targetCharacterRelation.CharacterId);

                if (account != null)
                {
                    WebApiAccess.Instance.Delete<CharacterRelation>(WebApiRoute.Relation, server.WebApi,
                        targetCharacterRelation.CharacterRelationId);
                    return;
                }
            }

            _characterRelationDao.Delete(targetCharacterRelation);
        }

        [Obsolete(
            "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
        public IEnumerable<IPacket> GenerateInv()
        {
            var inv0 = new InvPacket {Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket>()};
            var inv1 = new InvPacket {Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket>()};
            var inv2 = new InvPacket {Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket>()};
            var inv3 = new InvPacket {Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket>()};
            var inv6 = new InvPacket {Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket>()};
            var inv7 = new InvPacket {Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket>()};

            if (Inventory != null)
            {
                foreach (var inv in Inventory.Select(s => s.Value))
                {
                    switch (inv.Type)
                    {
                        case PocketType.Equipment:
                            if (inv.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                if (inv is SpecialistInstance specialistInstance)
                                {
                                    inv7.IvnSubPackets.Add(new IvnSubPacket
                                    {
                                        Slot = inv.Slot,
                                        VNum = inv.ItemVNum,
                                        RareAmount = specialistInstance.Rare,
                                        UpgradeDesign = specialistInstance.Upgrade,
                                        SecondUpgrade = specialistInstance.SpStoneUpgrade
                                    });
                                }
                            }
                            else
                            {
                                if (inv is WearableInstance wearableInstance)
                                {
                                    inv0.IvnSubPackets.Add(new IvnSubPacket
                                    {
                                        Slot = inv.Slot,
                                        VNum = inv.ItemVNum,
                                        RareAmount = wearableInstance.Rare,
                                        UpgradeDesign = inv.Item.IsColored ? wearableInstance.Design
                                            : wearableInstance.Upgrade
                                    });
                                }
                            }

                            break;

                        case PocketType.Main:
                            inv1.IvnSubPackets.Add(new IvnSubPacket
                                {Slot = inv.Slot, VNum = inv.ItemVNum, RareAmount = inv.Amount});
                            break;

                        case PocketType.Etc:
                            inv2.IvnSubPackets.Add(new IvnSubPacket
                                {Slot = inv.Slot, VNum = inv.ItemVNum, RareAmount = inv.Amount});
                            break;

                        case PocketType.Miniland:
                            inv3.IvnSubPackets.Add(new IvnSubPacket
                                {Slot = inv.Slot, VNum = inv.ItemVNum, RareAmount = inv.Amount});
                            break;

                        case PocketType.Specialist:
                            if (inv is SpecialistInstance specialist)
                            {
                                inv6.IvnSubPackets.Add(new IvnSubPacket
                                {
                                    Slot = inv.Slot,
                                    VNum = inv.ItemVNum,
                                    RareAmount = specialist.Rare,
                                    UpgradeDesign = specialist.Upgrade,
                                    SecondUpgrade = specialist.SpStoneUpgrade
                                });
                            }

                            break;

                        case PocketType.Costume:
                            if (inv is WearableInstance costumeInstance)
                            {
                                inv7.IvnSubPackets.Add(new IvnSubPacket
                                {
                                    Slot = inv.Slot,
                                    VNum = inv.ItemVNum,
                                    RareAmount = costumeInstance.Rare,
                                    UpgradeDesign = costumeInstance.Upgrade
                                });
                            }

                            break;

                        case PocketType.Wear:
                        case PocketType.Bazaar:
                        case PocketType.Warehouse:
                        case PocketType.FamilyWareHouse:
                        case PocketType.PetWarehouse:
                            break;
                        default:
                            _logger.Information(
                                LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.POCKETTYPE_UNKNOWN));
                            break;
                    }
                }
            }

            return new List<IPacket> {inv0, inv1, inv2, inv3, inv6, inv7};
        }

        public bool IsRelatedToCharacter(long characterId, CharacterRelationType relationType)
        {
            return CharacterRelations.Values.Any(s =>
                s.RelationType == relationType && s.RelatedCharacterId.Equals(characterId)
                && s.CharacterId.Equals(CharacterId));
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
                switch (IsReputHero())
                {
                    case 1:
                        return 28;
                    case 2:
                        return 29;
                    case 3:
                        return 30;
                    case 4:
                        return 31;
                    case 5:
                        return 32;
                    default:
                        return 27;
                }
            }

            return 0;
        }

        public void LoadSpeed()
        {
            Speed = CharacterHelper.Instance.SpeedData[(byte) Class];
        }

        public double MpLoad()
        {
            const int mp = 0;
            const double multiplicator = 1.0;
            return (int) ((CharacterHelper.Instance.MpData[(byte) Class][Level] + mp) * multiplicator);
        }

        public double HpLoad()
        {
            const double multiplicator = 1.0;
            const int hp = 0;

            return (int) ((CharacterHelper.Instance.HpData[(byte) Class][Level] + hp) * multiplicator);
        }

        public AtPacket GenerateAt()
        {
            return new AtPacket
            {
                CharacterId = CharacterId,
                MapId = MapId,
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
                ClassType = Session.GetMessageFromKey((LanguageKey) Enum.Parse(typeof(LanguageKey),
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
                Unknown2 = -1,
                FamilyId = -1,
                FamilyName = null,
                CharacterId = CharacterId,
                Authority = (byte) Account.Authority,
                Gender = Gender,
                HairStyle = HairStyle,
                HairColor = HairColor,
                Class = Class,
                Icon = (byte) (GetDignityIco() == 1 ? GetReputIco() : -GetDignityIco()),
                Compliment = (short) (Account.Authority == AuthorityType.Moderator ? 500 : Compliment),
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
            EquipmentSubPacket generateEquipmentSubPacket(EquipmentType eqType)
            {
                var eq = Inventory.LoadBySlotAndType<IItemInstance>((short) eqType, PocketType.Wear);
                if (eq == null)
                {
                    return null;
                }

                return new EquipmentSubPacket
                {
                    EquipmentType = eqType,
                    VNum = eq.ItemVNum,
                    Rare = eq.Rare,
                    Upgrade = (eq?.Item.IsColored == true ? eq?.Design : eq?.Upgrade) ?? 0,
                    Unknown = 0,
                };
            }

            return new EquipPacket
            {
                WeaponUpgradeRareSubPacket = WeaponUpgradeRareSubPacket,
                ArmorUpgradeRareSubPacket = ArmorUpgradeRareSubPacket,
                Armor = generateEquipmentSubPacket(EquipmentType.Armor),
                WeaponSkin = generateEquipmentSubPacket(EquipmentType.WeaponSkin),
                SecondaryWeapon = generateEquipmentSubPacket(EquipmentType.SecondaryWeapon),
                Sp = generateEquipmentSubPacket(EquipmentType.Sp),
                Amulet = generateEquipmentSubPacket(EquipmentType.Amulet),
                Boots = generateEquipmentSubPacket(EquipmentType.Boots),
                CostumeHat = generateEquipmentSubPacket(EquipmentType.CostumeHat),
                CostumeSuit = generateEquipmentSubPacket(EquipmentType.CostumeSuit),
                Fairy = generateEquipmentSubPacket(EquipmentType.Fairy),
                Gloves = generateEquipmentSubPacket(EquipmentType.Gloves),
                Hat = generateEquipmentSubPacket(EquipmentType.Hat),
                MainWeapon = generateEquipmentSubPacket(EquipmentType.MainWeapon),
                Mask = generateEquipmentSubPacket(EquipmentType.Mask),
                Necklace = generateEquipmentSubPacket(EquipmentType.Necklace),
                Ring = generateEquipmentSubPacket(EquipmentType.Ring),
                Bracelet = generateEquipmentSubPacket(EquipmentType.Bracelet),
            };
        }

        public EqPacket GenerateEq()
        {
            return new EqPacket
            {
                VisualId = VisualId,
                Visibility = (byte) (Authority < AuthorityType.GameMaster ? 0 : 2),
                Gender = Gender,
                HairStyle = HairStyle,
                Haircolor = HairColor,
                ClassType = Class,
                EqSubPacket = Equipment,
                WeaponUpgradeRarePacket = WeaponUpgradeRareSubPacket,
                ArmorUpgradeRarePacket = ArmorUpgradeRareSubPacket
            };
        }

        public void RemoveSp()
        {
            UseSp = false;
            Morph = 0;
            MorphUpgrade = 0;
            MorphDesign = 0;
            LoadSpeed();
            SendPacket(this.GenerateCond());
            SendPacket(GenerateLev());
            SpCooldown = 30;
            SendPacket(this.GenerateSay(
                string.Format(Language.Instance.GetMessageFromKey(LanguageKey.STAY_TIME, Account.Language), SpCooldown),
                SayColorType.Purple));
            SendPacket(new SdPacket {Cooldown = SpCooldown});
            MapInstance.Sessions.SendPacket(this.GenerateCMode());
            MapInstance.Sessions.SendPacket(new GuriPacket
            {
                Type = 6,
                Argument = 1,
                VisualEntityId = CharacterId
            });
            SendPacket(GenerateStat());

            Observable.Timer(TimeSpan.FromMilliseconds(SpCooldown * 1000)).Subscribe(o =>
            {
                SendPacket(this.GenerateSay(
                    string.Format(
                        Language.Instance.GetMessageFromKey(LanguageKey.TRANSFORM_DISAPPEAR, Account.Language),
                        SpCooldown), SayColorType.Purple));
                SendPacket(new SdPacket {Cooldown = 0});
            });
        }

        public void ChangeSp()
        {
            SpecialistInstance sp =
                Inventory.LoadBySlotAndType<SpecialistInstance>((byte) EquipmentType.Sp, PocketType.Wear);
            WearableInstance fairy =
                Inventory.LoadBySlotAndType<WearableInstance>((byte) EquipmentType.Fairy, PocketType.Wear);

            if (GetReputIco() < sp.Item.ReputationMinimum)
            {
                SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.LOW_REP,
                        Session.Account.Language)
                });
                return;
            }

            if (fairy != null && sp.Item.Element != 0 && fairy.Item.Element != sp.Item.Element &&
                fairy.Item.Element != sp.Item.SecondaryElement)
            {
                SendPacket(new MsgPacket
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
            MapInstance.Sessions.SendPacket(this.GenerateCMode());
            SendPacket(GenerateLev());
            MapInstance.Sessions.SendPacket(this.GenerateEff(196));
            MapInstance.Sessions.SendPacket(new GuriPacket
            {
                Type = 6,
                Argument = 1,
                VisualEntityId = CharacterId
            });
            SendPacket(GenerateSpPoint());
            LoadSpeed();
            SendPacket(this.GenerateCond());
            SendPacket(GenerateStat());
        }

        public void RemoveVehicle()
        {
            if (UseSp)
            {
                SpecialistInstance sp =
                    Inventory.LoadBySlotAndType<SpecialistInstance>((byte) EquipmentType.Sp, PocketType.Wear);
                if (sp != null)
                {
                    Morph = sp.Item.Morph;
                    MorphDesign = sp.Design;
                    MorphUpgrade = sp.Upgrade;
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
            SendPacket(this.GenerateCond());
            MapInstance.Sessions.SendPacket(this.GenerateCMode());
        }
    }
}