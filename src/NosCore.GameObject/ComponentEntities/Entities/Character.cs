//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ShopService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.ComponentEntities.Entities
{
    public class Character(IInventoryService inventory, IExchangeService exchangeService,
            IItemGenerationService itemProvider,
            IHpService hpService, IMpService mpService,
            IReputationService reputationService, IDignityService dignityService,
            ISpeedCalculationService speedCalculationService,
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

        public long BankGold
        {
            get => Account.BankMoney;
            set => Account.BankMoney = value;
        }

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

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            return gameLanguageLocalizer[languageKey, AccountLanguage];
        }
    }
}
