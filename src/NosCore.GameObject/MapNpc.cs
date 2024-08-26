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

using Mapster;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.NRunService;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NodaTime;
using System.Threading;

namespace NosCore.GameObject
{
    public class MapNpc(IItemGenerationService? itemProvider, ILogger logger, IHeuristic distanceCalculator,
            IClock clock)
        : MapNpcDto, INonPlayableEntity, IRequestableEntity
    {
        public NpcMonsterDto NpcMonster { get; private set; } = null!;

        public IDisposable? Life { get; private set; }
        public ConcurrentDictionary<IAliveEntity, int> HitList => new();

        public void Initialize(NpcMonsterDto npcMonster, ShopDto? shopDto, NpcTalkDto? npcTalkDto, List<ShopItemDto> shopItemsDto)
        {
            NpcMonster = npcMonster;
            Mp = NpcMonster?.MaxMp ?? 0;
            Hp = NpcMonster?.MaxHp ?? 0;
            Speed = NpcMonster?.Speed ?? 0;
            PositionX = MapX;
            PositionY = MapY;
            IsAlive = true;

            Task RequestExecAsync(RequestData request)
            {
                return ShowDialogAsync(request);
            }
            Requests[typeof(INrunEventHandler)]?.Select(RequestExecAsync).Subscribe();
            var shopObj = shopDto;
            if (shopObj == null)
            {
                return;
            }

            var shopItemsList = new ConcurrentDictionary<int, ShopItem>();
            Parallel.ForEach(shopItemsDto, shopItemGrouping =>
            {
                var shopItem = shopItemGrouping.Adapt<ShopItem>();
                shopItem.ItemInstance = itemProvider!.Create(shopItemGrouping.ItemVNum, -1);
                shopItemsList[shopItemGrouping.ShopItemId] = shopItem;
            });
            Shop = shopObj.Adapt<Shop>();
            Shop.Name = npcTalkDto?.Name ?? new I18NString();
            Shop.Session = null;
            Shop.ShopItems = shopItemsList;
        }
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
        public Shop? Shop { get; private set; }

        public Dictionary<Type, Subject<RequestData>> Requests { get; set; } = new()
        {
            [typeof(INrunEventHandler)] = new()
        };

        private Task ShowDialogAsync(RequestData requestData)
        {
            return requestData.ClientSession.SendPacketAsync(this.GenerateNpcReq(Dialog ?? 0));
        }

        internal void StopLife()
        {
            Life?.Dispose();
            Life = null;
        }

        public Task StartLifeAsync()
        {
            async Task LifeAsync()
            {
                try
                {
                    if (!MapInstance.IsSleeping)
                    {
                        await MonsterLifeAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e.Message, e);
                }
            }
            Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Select(_ => LifeAsync()).Subscribe();
            return Task.CompletedTask;
        }

        private Task MonsterLifeAsync()
        {
            return this.MoveAsync(distanceCalculator, clock);
        }
    }
}