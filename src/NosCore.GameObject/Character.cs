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
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using NodaTime;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;

namespace NosCore.GameObject
{
    public class Character : CharacterDto, ICharacterEntity
    {
        private readonly ILogger _logger;
        private readonly IHpService _hpService;
        private readonly IMpService _mpService;
        private readonly IExperienceService _experienceService;
        private readonly IJobExperienceService _jobExperienceService;
        private readonly IHeroExperienceService _heroExperienceService;
        private readonly IReputationService _reputationService;
        private readonly IDignityService _dignityService;
        private readonly IOptions<WorldConfiguration> _worldConfiguration;
        private readonly IClock _clock;
        private readonly ISpeedCalculationService _speedCalculationService;

        public Character(IInventoryService inventory, IExchangeService exchangeService, IItemGenerationService itemProvider, ILogger logger,
            IHpService hpService, IMpService mpService, IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService,
            IReputationService reputationService, IDignityService dignityService, IOptions<WorldConfiguration> worldConfiguration, IClock clock, ISpeedCalculationService speedCalculationService)
        {
            InventoryService = inventory;
            ExchangeProvider = exchangeService;
            ItemProvider = itemProvider;
            GroupRequestCharacterIds = new ConcurrentDictionary<long, long>();
            Group = new Group(GroupType.Group);
            _logger = logger;
            _hpService = hpService;
            _mpService = mpService;
            _experienceService = experienceService;
            _jobExperienceService = jobExperienceService;
            _heroExperienceService = heroExperienceService;
            _reputationService = reputationService;
            _dignityService = dignityService;
            _worldConfiguration = worldConfiguration;
            LastSp = clock.GetCurrentInstant();
            _clock = clock;
            _speedCalculationService = speedCalculationService;
        }

        public ScriptDto? Script { get; set; }

        public bool IsChangingMapInstance { get; set; }

        public Instant LastPortal { get; set; }

        public ClientSession Session { get; set; } = null!;

        public Instant LastMove { get; set; }

        public IItemGenerationService ItemProvider { get; set; }

        public bool UseSp { get; set; }

        public Instant LastSp { get; set; }

        public short SpCooldown { get; set; }

        public bool IsVehicled { get; set; }

        public byte? VehicleSpeed { get; set; }

        public IExchangeService ExchangeProvider { get; }

        public bool InExchangeOrShop => InExchange || InShop;

        public bool InExchange => ExchangeProvider!.CheckExchange(VisualId);

        public bool InShop { get; set; }

        public List<QuicklistEntryDto> QuicklistEntries { get; set; } = new();

        public long BankGold => Session.Account.BankMoney;

        public RegionType AccountLanguage => Session.Account.Language;

        public ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; set; }

        public ConcurrentDictionary<Guid, CharacterQuest> Quests { get; set; } = null!;

        public Dictionary<Type, Subject<RequestData>> Requests { get; set; } = new()
        {
            [typeof(INrunEventHandler)] = new Subject<RequestData>()
        };

        public short Race => (byte)Class;

        public Shop? Shop { get; set; }

        public bool Camouflage { get; set; }

        public bool Invisible { get; set; }

        public IInventoryService InventoryService { get; }

        public Group? Group { get; set; }

        public Instant? LastGroupRequest { get; set; } = null;

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

        public byte Speed => _speedCalculationService.CalculateSpeed(this);

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

        public int MaxHp
        {
            get
            {
                const double multiplicator = 1.0;
                const int hp = 0;

                return (int)((_hpService.GetHp(Class, Level) + hp) * multiplicator);
            }
        }

        public int MaxMp
        {
            get
            {
                const int mp = 0;
                const double multiplicator = 1.0;
                return (int)((_mpService.GetMp(Class, Level) + mp) * multiplicator);
            }
        }

        public List<StaticBonusDto> StaticBonusList { get; set; } = new();
        public List<TitleDto> Titles { get; set; } = new();
        public bool IsDisconnecting { get; internal set; }

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
            await SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.HeroLevelIncreased
            }).ConfigureAwait(false);
        }

        public async Task SetJobLevelAsync(byte jobLevel)
        {
            JobLevel = (byte)((Class == CharacterClassType.Adventurer) && (jobLevel > 20) ? 20 : jobLevel);
            JobLevelXp = 0;
            await SendPacketAsync(this.GenerateLev(_experienceService, _jobExperienceService, _heroExperienceService)).ConfigureAwait(false);
            var mapSessions = Broadcaster.Instance.GetCharacters(s => s.MapInstance == MapInstance);
            await Task.WhenAll(mapSessions.Select(s =>
            {
                //if (s.VisualId != VisualId)
                //{
                //    TODO: Generate GIDX
                //}

                return s.SendPacketAsync(this.GenerateEff(8));
            })).ConfigureAwait(false);
            await SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.JobLevelIncreased
            }).ConfigureAwait(false);
        }

        public void JoinGroup(Group group)
        {
            Group = group;
            group.JoinGroup(this);
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
                    await groupMember.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.PartyDisbanded
                    }).ConfigureAwait(false);
                }

                await groupMember.SendPacketAsync(groupMember.Group!.GeneratePinit()).ConfigureAwait(false);
            }

            Group = new Group(GroupType.Group);
            Group.JoinGroup(this);
        }

        // todo move this
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
                await SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.RemoveEquipment
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
            await SendPacketAsync(this.GenerateLev(_experienceService, _jobExperienceService, _heroExperienceService)).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateCMode()).ConfigureAwait(false);
            await SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.ClassChanged
            }).ConfigureAwait(false);

            QuicklistEntries = new List<QuicklistEntryDto>
            {
                new()
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
                SayColorType.Red)).ConfigureAwait(false);
        }

        public async Task SetReputationAsync(long reput)
        {
            Reput = reput;
            await SendPacketAsync(this.GenerateFd()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateSay(
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.REPUTATION_CHANGED, Session.Account.Language),
                SayColorType.Red)).ConfigureAwait(false);
        }

        //todo move this
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
                        SayColorType.Red)).ConfigureAwait(false);
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
                    new()
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
            await SendPacketAsync(this.GenerateLev(_experienceService, _jobExperienceService, _heroExperienceService)).ConfigureAwait(false);
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
            await SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.LevelIncreased
            }).ConfigureAwait(false);
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
                HpMaximum = MaxHp,
                Mp = Mp,
                MpMaximum = MaxMp,
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
            await SendPacketAsync(this.GenerateCond()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateLev(_experienceService, _jobExperienceService, _heroExperienceService)).ConfigureAwait(false);
            SpCooldown = 30;
            await SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = CharacterId,
                Type = SayColorType.Red,
                Message = Game18NConstString.DurationOfSideEffect,
                FirstArgument = 4,
                SecondArgument = SpCooldown
            }).ConfigureAwait(false);
            await SendPacketAsync(new SdPacket { Cooldown = SpCooldown }).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateCMode()).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = CharacterId
            }).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateStat()).ConfigureAwait(false);

            async Task CoolDown()
            {
                await SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.TransformationSideEffectGone
                }).ConfigureAwait(false);
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
                await SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotBeWornReputationLow
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

            LastSp = _clock.GetCurrentInstant();
            UseSp = true;
            Morph = sp.Item.Morph;
            MorphUpgrade = sp.Upgrade;
            MorphDesign = sp.Design;
            await MapInstance.SendPacketAsync(this.GenerateCMode()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateLev(_experienceService, _jobExperienceService, _heroExperienceService)).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(this.GenerateEff(196)).ConfigureAwait(false);
            await MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Dance,
                Value = 1,
                EntityId = CharacterId
            }).ConfigureAwait(false);
            await SendPacketAsync(GenerateSpPoint()).ConfigureAwait(false);
            await SendPacketAsync(this.GenerateCond()).ConfigureAwait(false);
            await SendPacketAsync(GenerateStat()).ConfigureAwait(false);
        }

        public async Task RemoveVehicleAsync()
        {
            if (UseSp)
            {
                var sp = InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear);
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