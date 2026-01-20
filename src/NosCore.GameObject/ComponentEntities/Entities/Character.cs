//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.ShopService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.ComponentEntities.Entities
{
    public class Character(IInventoryService inventory, IExchangeService exchangeService,
            IItemGenerationService itemProvider,
            IHpService hpService, IMpService mpService, IExperienceService experienceService,
            IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService,
            IReputationService reputationService, IDignityService dignityService,
            IOptions<WorldConfiguration> worldConfiguration, ISpeedCalculationService speedCalculationService,
            ISessionRegistry sessionRegistry,
            IGameLanguageLocalizer gameLanguageLocalizer)
        : CharacterDto, ICharacterEntity
    {
        public ScriptDto? Script { get; set; }
        public ConcurrentDictionary<IAliveEntity, int> HitList => new();


        public ConcurrentDictionary<short, CharacterSkill> Skills { get; } = new();

        public bool IsChangingMapInstance { get; set; }

        public Instant LastPortal { get; set; }

        public AccountDto Account { get; set; } = null!;

        public IChannel? Channel { get; set; }

        public Instant LastMove { get; set; }

        public IItemGenerationService ItemProvider { get; set; } = itemProvider;

        public bool UseSp { get; set; }

        public Instant LastSp { get; set; }

        public short SpCooldown { get; set; }

        public bool IsVehicled { get; set; }

        public byte? VehicleSpeed { get; set; }

        public IExchangeService ExchangeProvider { get; } = exchangeService;

        public bool InExchangeOrShop => InExchange || InShop;

        public bool InExchange => ExchangeProvider.CheckExchange(VisualId);

        public bool InShop { get; set; }

        public List<QuicklistEntryDto> QuicklistEntries { get; set; } = new();

        public long BankGold => Account.BankMoney;

        public RegionType AccountLanguage => Account.Language;

        public ConcurrentDictionary<long, long> GroupRequestCharacterIds { get; set; } = new();

        public ConcurrentDictionary<Guid, CharacterQuest> Quests { get; set; } = null!;

        public Dictionary<Type, Subject<RequestData>> Requests { get; set; } = new()
        {
            [typeof(INrunEventHandler)] = new Subject<RequestData>()
        };

        public short Race => (byte)Class;

        public Shop? Shop { get; set; }

        public bool Camouflage { get; set; }

        public bool Invisible { get; set; }

        public IInventoryService InventoryService { get; } = inventory;

        public Group? Group { get; set; }

        public Instant? LastGroupRequest { get; set; } = null;

        public ReputationType ReputIcon => reputationService.GetLevelFromReputation(Reput);

        public DignityType DignityIcon => dignityService.GetLevelFromDignity(Dignity);

        public MapInstance MapInstance { get; set; } = null!;

        public VisualType VisualType => VisualType.Player;

        public short VNum { get; set; }

        public long VisualId => CharacterId;

        public byte Direction { get; set; }

        public byte Size { get; set; } = 100;

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public byte Speed => speedCalculationService.CalculateSpeed(this);

        public short Morph { get; set; }

        public byte MorphUpgrade { get; set; }

        public short MorphDesign { get; set; }

        public byte MorphBonus { get; set; }

        public bool NoAttack { get; set; }

        public bool NoMove { get; set; }

        public bool IsSitting { get; set; }

        public Guid MapInstanceId => MapInstance.MapInstanceId;

        public AuthorityType Authority => Account.Authority;

        public bool IsAlive { get; set; }

        public SemaphoreSlim HitSemaphore { get; } = new SemaphoreSlim(1, 1);

        public int MaxHp
        {
            get
            {
                const double multiplicator = 1.0;
                const int hp = 0;

                return (int)((hpService.GetHp(Class, Level) + hp) * multiplicator);
            }
        }

        public int MaxMp
        {
            get
            {
                const int mp = 0;
                const double multiplicator = 1.0;
                return (int)((mpService.GetMp(Class, Level) + mp) * multiplicator);
            }
        }

        public List<StaticBonusDto> StaticBonusList { get; set; } = new();
        public List<TitleDto> Titles { get; set; } = new();
        public bool IsDisconnecting { get; internal set; }
        public bool CanFight { get; set; } = true;

        public Task SendPacketAsync(IPacket? packetDefinition)
        {
            var sender = sessionRegistry.GetSenderByCharacterId(CharacterId);
            return sender?.SendPacketAsync(packetDefinition) ?? Task.CompletedTask;
        }

        public Task SendPacketsAsync(IEnumerable<IPacket?> packetDefinitions)
        {
            var sender = sessionRegistry.GetSenderByCharacterId(CharacterId);
            return sender?.SendPacketsAsync(packetDefinitions) ?? Task.CompletedTask;
        }


        // todo move this
        public async Task ChangeClassAsync(CharacterClassType classType)
        {
            if (InventoryService.Any(s => s.Value.Type == NoscorePocketType.Wear))
            {
                await SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.RemoveEquipment
                });
                return;
            }

            JobLevel = 1;
            JobLevelXp = 0;
            await SendPacketAsync(new NpInfoPacket());
            await SendPacketAsync(new PclearPacket());

            if (classType == CharacterClassType.Adventurer)
            {
                HairStyle = HairStyle > HairStyleType.HairStyleB ? 0 : HairStyle;
            }

            Class = classType;
            Hp = MaxHp;
            Mp = MaxMp;
            var itemsToAdd = new List<BasicEquipment>();
            foreach (var (key, _) in worldConfiguration.Value.BasicEquipments)
            {
                switch (key)
                {
                    case nameof(CharacterClassType.Adventurer) when Class == CharacterClassType.Adventurer:
                    case nameof(CharacterClassType.Archer) when Class == CharacterClassType.Archer:
                    case nameof(CharacterClassType.Mage) when Class == CharacterClassType.Mage:
                    case nameof(CharacterClassType.MartialArtist) when Class == CharacterClassType.MartialArtist:
                    case nameof(CharacterClassType.Swordsman) when Class == CharacterClassType.Swordsman:
                        itemsToAdd.AddRange(worldConfiguration.Value.BasicEquipments[key]);
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
                    inv!.Select(invItem => invItem.GeneratePocketChange((PocketType)invItem.Type, invItem.Slot)));
            }

            await SendPacketAsync(this.GenerateTit());
            await SendPacketAsync(this.GenerateStat());
            await MapInstance.SendPacketAsync(this.GenerateEq());
            await MapInstance.SendPacketAsync(this.GenerateEff(8));
            //TODO: Faction
            await SendPacketAsync(this.GenerateCond());
            await SendPacketAsync(this.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
            await SendPacketAsync(this.GenerateCMode());
            await SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.ClassChanged
            });

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

            await MapInstance.SendPacketAsync(this.GenerateIn(Prefix ?? ""), new EveryoneBut(Channel!.Id));
            await MapInstance.SendPacketAsync(Group!.GeneratePidx(this));
            await MapInstance.SendPacketAsync(this.GenerateEff(6));
            await MapInstance.SendPacketAsync(this.GenerateEff(198));
        }


        public void AddBankGold(long bankGold)
        {
            Account.BankMoney += bankGold;
        }

        public void RemoveBankGold(long bankGold)
        {
            Account.BankMoney -= bankGold;
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
                        await SendPacketAsync(mail.GeneratePost(0));
                    }
                    else
                    {
                        await SendPacketAsync(mail.GeneratePost(1));
                    }
                }
                else
                {
                    if (mail.ItemInstance != null)
                    {
                        await SendPacketAsync(mail.GeneratePost(3));
                    }
                    else
                    {
                        await SendPacketAsync(mail.GeneratePost(2));
                    }
                }
            }
        }

        public Task ChangeMapAsync(IMapChangeService mapChangeService, short mapId, short mapX, short mapY)
        {
            return mapChangeService.ChangeMapByCharacterIdAsync(CharacterId, mapId, mapX, mapY);
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            return gameLanguageLocalizer[languageKey, AccountLanguage];
        }

        public async Task CloseShopAsync()
        {
            Shop = null;

            await MapInstance.SendPacketAsync(this.GenerateShop(AccountLanguage));
            await MapInstance.SendPacketAsync(this.GeneratePFlag());

            IsSitting = false;
            await SendPacketAsync(this.GenerateCond());
            await MapInstance.SendPacketAsync(this.GenerateRest());
        }

        public async Task BuyAsync(Shop shop, short slot, short amount)
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
                await SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.FailNpc,
                    Message = Game18NConstString.NotEnoughGold5
                });
                return;
            }

            if (reputprice > Reput)
            {
                await SendPacketAsync(new SMemoiPacket
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
                inv = InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                    ItemProvider.Create(item.ItemInstance!.ItemVNum, amount), CharacterId));
            }
            else
            {
                if (price + shop.OwnerCharacter.Gold > worldConfiguration.Value.MaxGoldAmount)
                {
                    await SendPacketAsync(new SMemoPacket
                    {
                        Type = SMemoType.FailPlayer,
                        Message = GetMessageFromKey(LanguageKey.TOO_RICH_SELLER)
                    });
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
                var packet = await (shop.OwnerCharacter == null ? Task.FromResult((NInvPacket?)null) : shop.OwnerCharacter.BuyFromAsync(item, amount, slotChar));
                if (packet != null)
                {
                    await SendPacketAsync(packet);
                }

                await SendPacketsAsync(inv.Select(invItem => invItem.GeneratePocketChange((PocketType)invItem.Type, invItem.Slot)));
                await SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.SuccessNpc,
                    Message = Game18NConstString.TradeSuccessfull
                });

                if (reputprice == 0)
                {
                    Gold -= (long)(price * percent);
                    await SendPacketAsync(this.GenerateGold());
                }
                else
                {
                    Reput -= reputprice;
                    await SendPacketAsync(this.GenerateFd());
                    await SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = CharacterId,
                        Type = SayColorType.Red,
                        Message = Game18NConstString.ReputationReduced,
                        ArgumentType = 4,
                        Game18NArguments = { reputprice }
                    });
                }
            }
            else
            {
                await SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
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

            await SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)type, slotChar));
            var sellAmount = (item?.Price ?? 0) * amount;
            Gold += sellAmount;
            await SendPacketAsync(this.GenerateGold());
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
            });

            if (!Shop.ShopItems.IsEmpty)
            {
                return this.GenerateNInv(1, 0);
            }

            await CloseShopAsync();
            return null;

        }

        private async Task GenerateLevelupPacketsAsync()
        {
            await SendPacketAsync(this.GenerateStat());
            await SendPacketAsync(this.GenerateStatInfo());
            await SendPacketAsync(this.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
            var mapSessions = sessionRegistry.GetCharacters(s => s.MapInstance == MapInstance);

            await Task.WhenAll(mapSessions.Select(async s =>
            {
                if (s.VisualId != VisualId)
                {
                    await s.SendPacketAsync(this.GenerateIn(Authority == AuthorityType.Moderator
                        ? GetMessageFromKey(LanguageKey.SUPPORT) : string.Empty));
                    //TODO: Generate GIDX
                }

                await s.SendPacketAsync(this.GenerateEff(6));
                await s.SendPacketAsync(this.GenerateEff(198));
            }));

            foreach (var member in Group!.Keys)
            {
                var groupMember = sessionRegistry.GetCharacter(s =>
                    (s.VisualId == member.Item2) && (member.Item1 == VisualType.Player));

                groupMember?.SendPacketAsync(groupMember.Group!.GeneratePinit());
            }

            await SendPacketAsync(Group.GeneratePinit());
        }

        public async Task SetLevelAsync(byte level)
        {
            this.SetLevel(level);
            await GenerateLevelupPacketsAsync();
            await SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.LevelIncreased
            });
        }


    }
}
