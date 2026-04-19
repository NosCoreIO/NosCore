//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Entities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;

namespace NosCore.GameObject.Map
{
    public class MapNpc : MapNpcDto, INonPlayableEntity, IRequestableEntity
    {
        public NpcMonsterDto NpcMonster { get; set; } = null!;

        public IDisposable? Life { get; set; }
        public ConcurrentDictionary<IAliveEntity, int> HitList => new();

        public SemaphoreSlim HitSemaphore { get; } = new SemaphoreSlim(1, 1);

        public byte Speed { get; set; }
        public byte Size { get; set; } = 10;
        public int Mp { get; set; }
        public int Hp { get; set; }
        public short Morph { get; set; }
        public byte MorphUpgrade { get; set; }
        public short MorphDesign { get; set; }
        public byte MorphBonus { get; set; }
        public bool NoAttack { get; set; }
        public bool NoMove { get; set; }
        public VisualType VisualType => VisualType.Npc;
        public long VisualId => MapNpcId;

        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public MapInstance MapInstance { get; set; } = null!;
        public Instant LastMove { get; set; }
        public bool IsAlive { get; set; }

        public short Race => NpcMonster.Race;

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        public byte Level { get; set; }

        public byte HeroLevel { get; set; }
        public Shop? Shop { get; set; }

        public Dictionary<Type, Subject<RequestData>> Requests { get; set; } = new()
        {
            [typeof(INrunEventHandler)] = new()
        };

        public void Initialize(NpcMonsterDto npcMonster, ShopDto? shopDto, NpcTalkDto? dialog, List<ShopItemDto> shopItems, IItemGenerationService itemProvider)
        {
            NpcMonster = npcMonster;
            Hp = npcMonster.MaxHp;
            Mp = npcMonster.MaxMp;
            IsAlive = true;
            PositionX = MapX;
            PositionY = MapY;
            Speed = npcMonster.Speed;

            if (shopDto == null)
            {
                return;
            }

            var shopItemsList = new ConcurrentDictionary<int, ShopItem>();
            foreach (var shopItemDto in shopItems)
            {
                var shopItem = shopItemDto.Adapt<ShopItem>();
                shopItem.ItemInstance = itemProvider.Create(shopItemDto.ItemVNum, -1);
                shopItemsList[shopItemDto.ShopItemId] = shopItem;
            }
            Shop = shopDto.Adapt<Shop>();
            Shop.Name = dialog?.Name ?? new I18NString();
            Shop.ShopItems = shopItemsList;
        }
    }
}
