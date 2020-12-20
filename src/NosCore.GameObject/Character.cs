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
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Options;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.SpeedService;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
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
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Configuration;
using NosCore.GameObject.Providers.QuestProvider;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.GameObject
{
    public class Character : CharacterDto, ICharacterEntity
    {
        private readonly IDao<AccountDto, long> _accountDao;
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<InventoryItemInstanceDto, Guid> _inventoryItemInstanceDao;
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly ILogger _logger;
        private readonly IDao<MinilandDto, Guid> _minilandDao;
        private readonly IMinilandProvider _minilandProvider;
        private readonly IDao<QuicklistEntryDto, Guid> _quicklistEntriesDao;
        private readonly IDao<StaticBonusDto, long> _staticBonusDao;
        private readonly IDao<TitleDto, Guid> _titleDao;
        private readonly IDao<CharacterQuestDto, Guid> _characterQuestsDao;
        private readonly IHpService _hpService;
        private readonly IMpService _mpService;
        private readonly ISpeedService _speedService;
        private readonly IExperienceService _experienceService;
        private readonly IJobExperienceService _jobExperienceService;
        private readonly IHeroExperienceService _heroExperienceService;
        private readonly IReputationService _reputationService;
        private readonly IDignityService _dignityService;
        private readonly IOptions<WorldConfiguration> _worldConfiguration;

        public Character(IInventoryService inventory, IExchangeProvider exchangeProvider, IItemProvider itemProvider,
            IDao<CharacterDto, long> characterDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<AccountDto, long> accountDao,
            ILogger logger, IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quicklistEntriesDao, IDao<MinilandDto, Guid> minilandDao,
            IMinilandProvider minilandProvider, IDao<TitleDto, Guid> titleDao, IDao<CharacterQuestDto, Guid> characterQuestDao,
            IHpService hpService, IMpService mpService, IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISpeedService speedService,
            IReputationService reputationService, IDignityService dignityService, IOptions<WorldConfiguration> worldConfiguration)
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
            _characterQuestsDao = characterQuestDao;
            _minilandDao = minilandDao;
            _minilandProvider = minilandProvider;
            _hpService = hpService;
            _mpService = mpService;
            _experienceService = experienceService;
            _jobExperienceService = jobExperienceService;
            _heroExperienceService = heroExperienceService;
            _speedService = speedService;
            _reputationService = reputationService;
            _dignityService = dignityService;
            _worldConfiguration = worldConfiguration;
        }

        private byte _speed;

        public ScriptDto? Script { get; set; }

        public bool IsChangingMapInstance { get; set; }

        public DateTime LastPortal { get; set; }

        public ClientSession Session { get; set; } = null!;

        public DateTime LastSpeedChange { get; set; }

        public DateTime LastMove { get; set; }

        public IItemProvider ItemProvider { get; set; }

        public bool UseSp { get; set; }

        public DateTime LastSp { get; set; } = SystemTime.Now();

        public short SpCooldown { get; set; }

        public bool IsVehicled { get; set; }

        public byte? VehicleSpeed { get; set; }

        public IExchangeProvider ExchangeProvider { get; }

        public bool InExchangeOrShop => InExchange || InShop;

        public bool InExchange => ExchangeProvider!.CheckExchange(VisualId);

        public bool InShop { get; set; }

        public List<QuicklistEntryDto> QuicklistEntries { get; set; }

        public long BankGold => Session.Account.BankMoney;

        public RegionType AccountLanguage => Session.Account.Language;

        public ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; set; }

        public ConcurrentDictionary<Guid, CharacterQuest> Quests { get; set; } = null!;

        public Subject<RequestData>? Requests { get; set; }

        public short Race => (byte)Class;

        public Shop? Shop { get; set; }

        public bool Camouflage { get; set; }

        public bool Invisible { get; set; }

        public IInventoryService InventoryService { get; }

        public Group? Group { get; set; }

        public DateTime? LastGroupRequest { get; set; } = null;

        public ReputationType ReputIcon => _reputationService.GetLevelFromReputation(Reput);

        public DignityType DignityIcon => _dignityService.GetLevelFromDignity(Dignity);

        public IChannel? Channel => Session?.Channel;

        public MapInstance MapInstance { get; set; } = null!;

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

        public AuthorityType Authority => Session.Account.Authority;

        public bool IsAlive { get; set; }

        public int MaxHp => (int)HpLoad();

        public int MaxMp => (int)MpLoad();

        public Task SendPacketAsync(IPacket? packetDefinition)
        {
            return Session.SendPacketAsync(packetDefinition);
        }

        public Task SendPacketsAsync(IEnumerable<IPacket?> packetDefinitions)
        {
            return Session.SendPacketsAsync(packetDefinitions);
        }

        public async Task SetHeroLevelAsync(byte level)
        {
            HeroLevel = level;
            HeroXp = 0;
            await GenerateLevelupPacketsAsync().ConfigureAwait(false);
            await SendPacketAsync(new MsgPacket
            {
                Type = MessageType.White,
                Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.HERO_LEVEL_CHANGED, Session.Account.Language)
            }).ConfigureAwait(false);
        }

        public async Task SetJobLevelAsync(byte jobLevel)
        {
            JobLevel = (byte)((Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20
                : jobLevel);
            JobLevelXp = 0;
            await SendPacketAsync(GenerateLev()).ConfigureAwait(false);
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);
            await Task.WhenAll(mapSessions.Select(s =>
            {
                //if (s.VisualId != VisualId)
                //{
                //    TODO: Generate GIDX
                //}

                return s.SendPacketAsync(this.GenerateEff(8));
            })).ConfigureAwait(false);
            await SendPacketAsync(new MsgPacket
            {
                Type = MessageType.White,
                Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.JOB_LEVEL_CHANGED, Session.Account.Language)
            }).ConfigureAwait(false);
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

        public async Task LeaveGroupAsync()
        {
            Group!.LeaveGroup(this);
            foreach (var member in Group.Keys.Where(s => (s.Item2 != CharacterId) || (s.Item1 != VisualType.Player)))
            {
                var groupMember = Broadcaster.Instance.GetCharacter(s =>
                    (s.VisualId == member.Item2) && (member.Item1 == VisualType.Player));

                if (groupMember == null)
                {
                    continue;
                }

                if (Group.Count == 1)
                {
                    await groupMember.LeaveGroupAsync().ConfigureAwait(false);
                    await groupMember.SendPacketAsync(Group.GeneratePidx(groupMember)).ConfigureAwait(false);
                    await groupMember.SendPacketAsync(new MsgPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_CLOSED,
                            groupMember.AccountLanguage),
                        Type = MessageType.White
                    }).ConfigureAwait(false);
                }

                await groupMember.SendPacketAsync(groupMember.Group!.GeneratePinit()).ConfigureAwait(false);
            }

            Group = new Group(GroupType.Group);
            Group.JoinGroup(this);
        }

        public async Task SaveAsync()
        {
            try
            {
                var account = Session.Account;
                await _accountDao.TryInsertOrUpdateAsync(account).ConfigureAwait(false);

                CharacterDto character = (Character)MemberwiseClone();
                await _characterDao.TryInsertOrUpdateAsync(character).ConfigureAwait(false);

                var quicklistEntriesToDelete = _quicklistEntriesDao
                        .Where(i => i.CharacterId == CharacterId)!.ToList()
                    .Where(i => QuicklistEntries.All(o => o.Id != i.Id)).ToList();
                await _quicklistEntriesDao.TryDeleteAsync(quicklistEntriesToDelete.Select(s => s.Id).ToArray()).ConfigureAwait(false);
                await _quicklistEntriesDao.TryInsertOrUpdateAsync(QuicklistEntries).ConfigureAwait(false);

                // load and concat inventory with equipment
                var itemsToDelete = _inventoryItemInstanceDao
                        .Where(i => i.CharacterId == CharacterId)!.ToList()
                    .Where(i => InventoryService.Values.All(o => o.Id != i.Id)).ToList();

                await _inventoryItemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.Id).ToArray()).ConfigureAwait(false);
                await _itemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.ItemInstanceId).ToArray()).ConfigureAwait(false);

                await _itemInstanceDao.TryInsertOrUpdateAsync(InventoryService.Values.Select(s => s.ItemInstance!).ToArray()).ConfigureAwait(false);
                await _inventoryItemInstanceDao.TryInsertOrUpdateAsync(InventoryService.Values.ToArray()).ConfigureAwait(false);

                var staticBonusToDelete = _staticBonusDao
                        .Where(i => i.CharacterId == CharacterId)!.ToList()
                    .Where(i => StaticBonusList.All(o => o.StaticBonusId != i.StaticBonusId)).ToList();
                await _staticBonusDao.TryDeleteAsync(staticBonusToDelete.Select(s => s.StaticBonusId)).ConfigureAwait(false);
                await _staticBonusDao.TryInsertOrUpdateAsync(StaticBonusList).ConfigureAwait(false);

                await _titleDao.TryInsertOrUpdateAsync(Titles).ConfigureAwait(false);

                var minilandDto = (MinilandDto)_minilandProvider.GetMiniland(CharacterId);
                await _minilandDao.TryInsertOrUpdateAsync(minilandDto).ConfigureAwait(false);

                var questsToDelete = _characterQuestsDao
                        .Where(i => i.CharacterId == CharacterId)!.ToList()
                    .Where(i => Quests.Values.All(o => o.QuestId != i.QuestId)).ToList();
                await _characterQuestsDao.TryDeleteAsync(questsToDelete.Select(s => s.Id)).ConfigureAwait(false);
                await _characterQuestsDao.TryInsertOrUpdateAsync(Quests.Values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error("Save Character failed. SessionId: " + Session.SessionId, e);
            }
        }


        public List<StaticBonusDto> StaticBonusList { get; set; } = new List<StaticBonusDto>();
        public List<TitleDto> Titles { get; set; } = new List<TitleDto>();
        public bool IsDisconnecting { get; internal set; }

        public async Task ChangeClassAsync(CharacterClassType classType)
        {
            if (Class == classType)
            {
                _logger.Error(
                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_CHANGE_SAME_CLASS, Session.Account.Language));
                return;
            }

            if (InventoryService.Any(s => s.Value.Type == NoscorePocketType.Wear))
            {
                await SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.EQ_NOT_EMPTY,
                        AccountLanguage),
                    Type = MessageType.White
                }).ConfigureAwait(false);
                return;
            }


            JobLevel = 1;
            JobLevelXp = 0;
            await SendPacketAsync(new NpInfoPacket()).ConfigureAwait(false);
            await SendPacketAsync(new PclearPacket()).ConfigureAwait(false);

            if (classType == CharacterClassType.Adventurer)
            {
                HairStyle = HairStyle > HairStyleType.HairStyleB ? 0 : HairStyle;
            }

            LoadSpeed();

            Class = classType;
            Hp = MaxHp;
            Mp = MaxMp;
            var itemsToAdd = new List<BasicEquipment>();
            foreach (var (key, _) in _worldConfiguration.Value.BasicEquipments)
            {
                switch (key)
                {
                    case nameof(CharacterClassType.Adventurer) when Class == CharacterClassType.Adventurer:
                    case nameof(CharacterClassType.Archer) when Class == CharacterClassType.Archer:
                    case nameof(CharacterClassType.Mage) when Class == CharacterClassType.Mage:
                    case nameof(CharacterClassType.MartialArtist) when Class == CharacterClassType.MartialArtist:
                    case nameof(CharacterClassType.Swordsman) when Class == CharacterClassType.Swordsman:
                        itemsToAdd.AddRange(_worldConfiguration.Value.BasicEquipments[key]);
                        break;
                    default:
                        break;
                }
            }

            foreach (var inv in itemsToAdd
                .Select(itemToAdd => InventoryService.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(itemToAdd.VNum, itemToAdd.Amount), CharacterId), itemToAdd.NoscorePocketType))
                .Where(inv => inv != null))
            {
                await SendPacketsAsync(
                    inv!.Select(invItem => invItem.GeneratePocketChange((PocketType)invItem.Type, invItem.Slot))).ConfigureAwait(false);
            }

            await SendPacketAsync(this.GenerateTit()).ConfigureAwait(false);
            await SendPacketAsync(GenerateStat()).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateEq()).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateEff(8)).ConfigureAwait(false);
            //TODO: Faction
            await SendPacketAsync(this.GenerateCond()).ConfigureAwait(false);
            await SendPacketAsync(GenerateLev()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateCMode()).ConfigureAwait(false);
            await SendPacketAsync(new MsgPacket
            {
                Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.CLASS_CHANGED, Session.Account.Language),
                Type = MessageType.White
            }).ConfigureAwait(false);

            QuicklistEntries = new List<QuicklistEntryDto>
            {
                new QuicklistEntryDto
                {
                    Id = Guid.NewGuid(),
                    CharacterId = CharacterId,
                    QuickListIndex = 0,
                    Slot = 9,
                    Type = 1,
                    IconType = 3,
                    IconVNum = 1
                }
            };

            await MapInstance.SendPacketAsync(this.GenerateIn(Prefix ?? ""), new EveryoneBut(Session!.Channel!.Id)).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(Group!.GeneratePidx(this)).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateEff(6)).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateEff(198)).ConfigureAwait(false);
        }

        public Task AddGoldAsync(long gold)
        {
            Gold += gold;
            return SendPacketAsync(this.GenerateGold());
        }

        public Task RemoveGoldAsync(long gold)
        {
            Gold -= gold;
            return SendPacketAsync(this.GenerateGold());
        }

        public void AddBankGold(long bankGold)
        {
            Session.Account.BankMoney += bankGold;
        }

        public void RemoveBankGold(long bankGold)
        {
            Session.Account.BankMoney -= bankGold;
        }

        public async Task SetGoldAsync(long gold)
        {
            Gold = gold;
            await SendPacketAsync(this.GenerateGold()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateSay(
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.UPDATE_GOLD, Session.Account.Language),
                SayColorType.Purple)).ConfigureAwait(false);
        }

        public async Task SetReputationAsync(long reput)
        {
            Reput = reput;
            await SendPacketAsync(this.GenerateFd()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateSay(
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.REPUTATION_CHANGED, Session.Account.Language),
                SayColorType.Purple)).ConfigureAwait(false);
        }
        public async Task GenerateMailAsync(IEnumerable<MailData> mails)
        {
            foreach (var mail in mails)
            {
                if (!mail.MailDto.IsSenderCopy && (mail.ReceiverName == Name))
                {
                    if (mail.ItemInstance != null)
                    {
                        await Session.SendPacketAsync(mail.GeneratePost(0)).ConfigureAwait(false);
                    }
                    else
                    {
                        await Session.SendPacketAsync(mail.GeneratePost(1)).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (mail.ItemInstance != null)
                    {
                        await Session.SendPacketAsync(mail.GeneratePost(3)).ConfigureAwait(false);
                    }
                    else
                    {
                        await Session.SendPacketAsync(mail.GeneratePost(2)).ConfigureAwait(false);
                    }
                }
            }
        }

        public Task ChangeMapAsync(short mapId, short mapX, short mapY)
        {
            return Session.ChangeMapAsync(mapId, mapX, mapY);
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            return Session.GetMessageFromKey(languageKey);
        }

        public async Task CloseShopAsync()
        {
            Shop = null;

            await MapInstance.SendPacketAsync(this.GenerateShop(AccountLanguage)).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GeneratePFlag()).ConfigureAwait(false);

            IsSitting = false;
            LoadSpeed();
            await SendPacketAsync(this.GenerateCond()).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateRest()).ConfigureAwait(false);
        }

        public async Task BuyAsync(Shop shop, short slot, short amount)
        {
            var item = shop.ShopItems.Values.FirstOrDefault(it => it.Slot == slot);
            if (item == null)
            {
                return;
            }

            var price = item.Price ?? item.ItemInstance!.Item!.Price * amount;
            var reputprice = item.Price == null ? item.ItemInstance!.Item!.ReputPrice * amount : 0;
            var percent = DignityIcon switch
            {
                DignityType.Dreadful => 1.1,
                DignityType.Unqualified => 1.2,
                DignityType.Failed => 1.5,
                DignityType.Useless => 1.5,
                _ => 1.0,
            };
            if (amount > item.Amount)
            {
                //todo LOG
                return;
            }

            if ((reputprice == 0) && (price * percent > Gold))
            {
                await SendPacketAsync(new SMemoPacket
                {
                    Type = SMemoType.FatalError,
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, Session.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            if (reputprice > Reput)
            {
                await SendPacketAsync(new SMemoPacket
                {
                    Type = SMemoType.FatalError,
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_REPUT, Session.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            short slotChar = item.Slot;
            List<InventoryItemInstance>? inv;
            if (shop.Session == null)
            {
                inv = InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                    ItemProvider.Create(item.ItemInstance!.ItemVNum, amount), CharacterId));
            }
            else
            {
                if (price + shop.Session.Character.Gold > _worldConfiguration.Value.MaxGoldAmount)
                {
                    await SendPacketAsync(new SMemoPacket
                    {
                        Type = SMemoType.FatalError,
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.TOO_RICH_SELLER, Session.Account.Language)
                    }).ConfigureAwait(false);
                    return;
                }

                if (amount == item.ItemInstance?.Amount)
                {
                    inv = InventoryService.AddItemToPocket(InventoryItemInstance.Create(item.ItemInstance,
                        CharacterId));
                }
                else
                {
                    inv = InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                        ItemProvider.Create(item.ItemInstance?.ItemVNum ?? 0, amount), CharacterId));
                }
            }

            if (inv?.Count > 0)
            {
                inv.ForEach(it => it.CharacterId = CharacterId);
                var packet = await (shop.Session == null ? Task.FromResult((NInvPacket?)null) : shop.Session.Character.BuyFromAsync(item, amount, slotChar)).ConfigureAwait(false);
                if (packet != null)
                {
                    await SendPacketAsync(packet).ConfigureAwait(false);
                }

                await SendPacketsAsync(
                    inv.Select(invItem => invItem.GeneratePocketChange((PocketType)invItem.Type, invItem.Slot))).ConfigureAwait(false);
                await SendPacketAsync(new SMemoPacket
                {
                    Type = SMemoType.Success,
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.BUY_ITEM_VALID, Session.Account.Language)
                }).ConfigureAwait(false);
                if (reputprice == 0)
                {
                    Gold -= (long)(price * percent);
                    await SendPacketAsync(this.GenerateGold()).ConfigureAwait(false);
                }
                else
                {
                    Reput -= reputprice;
                    await SendPacketAsync(this.GenerateFd()).ConfigureAwait(false);
                    await SendPacketAsync(this.GenerateSay(
                        GameLanguage.Instance.GetMessageFromKey(LanguageKey.REPUT_DECREASED, Session.Account.Language),
                        SayColorType.Purple)).ConfigureAwait(false);
                }
            }
            else
            {
                await SendPacketAsync(new MsgiPacket
                {
                    Message = Game18NConstString.NotEnoughSpace,
                    Type = 0
                }).ConfigureAwait(false);
            }
        }

        private async Task<NInvPacket?> BuyFromAsync(ShopItem item, short amount, short slotChar)
        {
            var type = item.Type;
            var itemInstance = amount == item.ItemInstance?.Amount
                ? InventoryService.DeleteById(item.ItemInstance.Id)
                : InventoryService.RemoveItemAmountFromInventory(amount, item.ItemInstance!.Id);
            var slot = item.Slot;
            item.Amount = (short)((item.Amount ?? 0) - amount);
            if ((item?.Amount ?? 0) == 0)
            {
                Shop!.ShopItems.TryRemove(slot, out _);
            }

            await SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)type, slotChar)).ConfigureAwait(false);
            await SendPacketAsync(new SMemoPacket
            {
                Type = SMemoType.Success,
                Message = string.Format(
                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.BUY_ITEM_FROM, Session.Account.Language), Name,
                    item!.ItemInstance.Item!.Name[Session.Account.Language], amount)
            }).ConfigureAwait(false);
            var sellAmount = (item?.Price ?? 0) * amount;
            Gold += sellAmount;
            await SendPacketAsync(this.GenerateGold()).ConfigureAwait(false);
            Shop!.Sell += sellAmount;

            await SendPacketAsync(new SellListPacket
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
            }).ConfigureAwait(false);

            if (!Shop.ShopItems.IsEmpty)
            {
                return this.GenerateNInv(1, 0);
            }

            await CloseShopAsync().ConfigureAwait(false);
            return null;

        }

        private async Task GenerateLevelupPacketsAsync()
        {
            await SendPacketAsync(GenerateStat()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateStatInfo()).ConfigureAwait(false);
            //Session.SendPacket(GenerateStatChar());
            await SendPacketAsync(GenerateLev()).ConfigureAwait(false);
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);

            await Task.WhenAll(mapSessions.Select(async s =>
            {
                if (s.VisualId != VisualId)
                {
                    await s.SendPacketAsync(this.GenerateIn(Authority == AuthorityType.Moderator
                        ? GameLanguage.Instance.GetMessageFromKey(LanguageKey.SUPPORT, Session.Account.Language) : string.Empty)).ConfigureAwait(false);
                    //TODO: Generate GIDX
                }

                await s.SendPacketAsync(this.GenerateEff(6)).ConfigureAwait(false);
                await s.SendPacketAsync(this.GenerateEff(198)).ConfigureAwait(false);
            })).ConfigureAwait(false);

            foreach (var member in Group!.Keys)
            {
                var groupMember = Broadcaster.Instance.GetCharacter(s =>
                    (s.VisualId == member.Item2) && (member.Item1 == VisualType.Player));

                groupMember?.SendPacketAsync(groupMember.Group!.GeneratePinit());
            }

            await SendPacketAsync(Group.GeneratePinit()).ConfigureAwait(false);
        }

        public async Task SetLevelAsync(byte level)
        {
            this.SetLevel(level);
            await GenerateLevelupPacketsAsync().ConfigureAwait(false);
            await SendPacketAsync(new MsgPacket
            {
                Type = MessageType.White,
                Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.LEVEL_CHANGED, Session.Account.Language)
            }).ConfigureAwait(false);
        }

        public LevPacket GenerateLev()
        {
            return new LevPacket
            {
                Level = Level,
                LevelXp = LevelXp,
                JobLevel = JobLevel,
                JobLevelXp = JobLevelXp,
                XpLoad = _experienceService.GetExperience(Level),
                JobXpLoad = _jobExperienceService.GetJobExperience(Class, JobLevel),
                Reputation = Reput,
                SkillCp = 0,
                HeroXp = HeroXp,
                HeroLevel = HeroLevel,
                HeroXpLoad = HeroLevel == 0 ? 0 : _heroExperienceService.GetHeroExperience(HeroLevel)
            };
        }

        public IEnumerable<QSlotPacket> GenerateQuicklist()
        {
            var pktQs = new QSlotPacket[2];
            for (var i = 0; i < pktQs.Length; i++)
            {
                var subpacket = new List<QsetClientSubPacket?>();
                for (var j = 0; j < 30; j++)
                {
                    var qi = QuicklistEntries.FirstOrDefault(n =>
                        (n.QuickListIndex == i) && (n.Slot == j) && (n.Morph == (UseSp ? Morph : 0)));

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

        [Obsolete(
            "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
        public IEnumerable<IPacket> GenerateInv()
        {
            var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket?>() };
            var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket?>() };

            if (InventoryService == null)
            {
                return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7 };
            }

            foreach (var inv in InventoryService.Select(s => s.Value))
            {
                switch (inv.Type)
                {
                    case NoscorePocketType.Equipment:
                        if (inv.ItemInstance!.Item!.EquipmentSlot == EquipmentType.Sp)
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
                            Slot = inv.Slot, VNum = inv.ItemInstance!.ItemVNum, RareAmount = inv.ItemInstance.Amount
                        });
                        break;

                    case NoscorePocketType.Etc:
                        inv2.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot, VNum = inv.ItemInstance!.ItemVNum, RareAmount = inv.ItemInstance.Amount
                        });
                        break;

                    case NoscorePocketType.Miniland:
                        inv3.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot, VNum = inv.ItemInstance!.ItemVNum, RareAmount = inv.ItemInstance.Amount
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

            return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7 };
        }
        public SpPacket GenerateSpPoint()
        {
            return new SpPacket
            {
                AdditionalPoint = SpAdditionPoint,
                MaxAdditionalPoint = _worldConfiguration.Value.MaxAdditionalSpPoints,
                SpPoint = SpPoint,
                MaxSpPoint = _worldConfiguration.Value.MaxSpPoints
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
        public Task AddSpPointsAsync(int spPointToAdd)
        {
            SpPoint = SpPoint + spPointToAdd > _worldConfiguration.Value.MaxSpPoints
                ? _worldConfiguration.Value.MaxSpPoints : SpPoint + spPointToAdd;
            return SendPacketAsync(this.GenerateSpPoint());
        }

        public void LoadSpeed()
        {
            Speed = _speedService.GetSpeed(Class);
        }

        public double MpLoad()
        {
            const int mp = 0;
            const double multiplicator = 1.0;
            return (int)((_mpService.GetMp(Class, Level) + mp) * multiplicator);
        }

        public double HpLoad()
        {
            const double multiplicator = 1.0;
            const int hp = 0;

            return (int)((_hpService.GetHp(Class, Level) + hp) * multiplicator);
        }

        public Task AddAdditionalSpPointsAsync(int spPointToAdd)
        {
            SpAdditionPoint = SpAdditionPoint + spPointToAdd > _worldConfiguration.Value.MaxAdditionalSpPoints
                ? _worldConfiguration.Value.MaxAdditionalSpPoints : SpAdditionPoint + spPointToAdd;
            return SendPacketAsync(GenerateSpPoint());
        }


        public async Task RemoveSpAsync()
        {
            UseSp = false;
            Morph = 0;
            MorphUpgrade = 0;
            MorphDesign = 0;
            LoadSpeed();
            await SendPacketAsync(this.GenerateCond()).ConfigureAwait(false);
            await SendPacketAsync(GenerateLev()).ConfigureAwait(false);
            SpCooldown = 30;
            await SendPacketAsync(this.GenerateSay(
                string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.STAY_TIME, Session.Account.Language), SpCooldown),
                SayColorType.Purple)).ConfigureAwait(false);
            await SendPacketAsync(new SdPacket { Cooldown = SpCooldown }).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateCMode()).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Unknow,
                Value = 1,
                EntityId = CharacterId
            }).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateStat()).ConfigureAwait(false);

            async Task CoolDown()
            {
                await SendPacketAsync(this.GenerateSay(
                    string.Format(
                        GameLanguage.Instance.GetMessageFromKey(LanguageKey.TRANSFORM_DISAPPEAR, Session.Account.Language),
                        SpCooldown), SayColorType.Purple)).ConfigureAwait(false);
                await SendPacketAsync(new SdPacket { Cooldown = 0 }).ConfigureAwait(false);
            }

            Observable.Timer(TimeSpan.FromMilliseconds(SpCooldown * 1000)).Select(_ => CoolDown()).Subscribe();
        }

        public async Task ChangeSpAsync()
        {
            if (!(InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance is
                SpecialistInstance sp))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.USE_SP_WITHOUT_SP_ERROR));
                return;
            }

            if ((byte)ReputIcon < sp.Item!.ReputationMinimum)
            {
                await SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.LOW_REP,
                        Session.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            if (InventoryService.LoadBySlotAndType((byte)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance is
                    WearableInstance fairy
                && (sp.Item.Element != 0) && (fairy.Item!.Element != sp.Item.Element)
                && (fairy.Item.Element != sp.Item.SecondaryElement))
            {
                await SendPacketAsync(new MsgiPacket
                {
                    Message = Game18NConstString.SpecialistAndFairyDifferentElement
                }).ConfigureAwait(false);
                return;
            }

            LastSp = SystemTime.Now();
            UseSp = true;
            Morph = sp.Item.Morph;
            MorphUpgrade = sp.Upgrade;
            MorphDesign = sp.Design;
            await MapInstance.SendPacketAsync(this.GenerateCMode()).ConfigureAwait(false);
            await SendPacketAsync(GenerateLev()).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateEff(196)).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Unknow,
                Value = 1,
                EntityId = CharacterId
            }).ConfigureAwait(false);
            await SendPacketAsync(GenerateSpPoint()).ConfigureAwait(false);
            LoadSpeed();
            await SendPacketAsync(this.GenerateCond()).ConfigureAwait(false);
            await SendPacketAsync(GenerateStat()).ConfigureAwait(false);
        }

        public async Task RemoveVehicleAsync()
        {
            if (UseSp)
            {
                var sp =
                    InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear);
                if (sp != null)
                {
                    Morph = sp.ItemInstance!.Item!.Morph;
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
            await SendPacketAsync(this.GenerateCond()).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateCMode()).ConfigureAwait(false);
        }
    }
}