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

using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Networking.SessionGroup;


namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public class MapInstance : IBroadcastable, IDisposable, IRequestableEntity<MapInstance>
    {
        public short MaxPacketsBuffer { get; } = 250;
        private readonly ILogger _logger;

        private readonly IMapItemGenerationService _mapItemGenerationService;
        private bool _isSleeping;
        private bool _isSleepingRequest;
        private ConcurrentDictionary<int, MapMonster> _monsters;

        private ConcurrentDictionary<int, MapNpc> _npcs;

        public ConcurrentDictionary<Guid, MapDesignObject> MapDesignObjects = new();

        private readonly IClock _clock;
        private readonly IMapChangeService _mapChangeService;
        private readonly ISessionRegistry _sessionRegistry;

        public MapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType type,
            IMapItemGenerationService mapItemGenerationService, ILogger logger, IClock clock, IMapChangeService mapChangeService,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry)
        {
            LastPackets = new ConcurrentQueue<IPacket>();
            XpRate = 1;
            DropRate = 1;
            ShopAllowed = shopAllowed;
            MapInstanceType = type;
            Map = map;
            MapInstanceId = guid;
            Portals = new List<Portal>();
            _monsters = new ConcurrentDictionary<int, MapMonster>();
            _npcs = new ConcurrentDictionary<int, MapNpc>();
            MapItems = new ConcurrentDictionary<long, MapItem>();
            _isSleeping = true;
            _clock = clock;
            LastUnregister = _clock.GetCurrentInstant().Plus(Duration.FromMinutes(-1));
            Sessions = sessionGroupFactory.Create();
            _mapItemGenerationService = mapItemGenerationService;
            _logger = logger;
            _mapChangeService = mapChangeService;
            _sessionRegistry = sessionRegistry;
            Requests = new Dictionary<Type, Subject<RequestData<MapInstance>>>
            {
                [typeof(IMapInstanceEntranceEventHandler)] = new()
            };
        }

        public Instant LastUnregister { get; set; }

        public ConcurrentDictionary<long, MapItem> MapItems { get; }

        public bool IsSleeping
        {
            get
            {
                if (!_isSleepingRequest || _isSleeping || (LastUnregister.Plus(Duration.FromSeconds(30)) >= _clock.GetCurrentInstant()))
                {
                    return _isSleeping;
                }

                _isSleeping = true;
                _isSleepingRequest = false;
                Parallel.ForEach(Monsters.Where(s => s.Life != null), monster => monster.StopLife());
                Parallel.ForEach(Npcs.Where(s => s.Life != null), npc => npc.StopLife());

                return true;
            }
            set
            {
                if (value)
                {
                    _isSleepingRequest = true;
                }
                else
                {
                    _isSleeping = false;
                    _isSleepingRequest = false;
                }
            }
        }

        public int DropRate { get; set; }

        public Map.Map Map { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType MapInstanceType { get; set; }

        public List<MapMonster> Monsters
        {
            get { return _monsters.Select(s => s.Value).ToList(); }
        }

        public List<MapNpc> Npcs
        {
            get { return _npcs.Select(s => s.Value).ToList(); }
        }

        public List<Portal> Portals { get; set; }

        public bool ShopAllowed { get; }

        public int XpRate { get; set; }

        private IDisposable? Life { get; set; }

        public ISessionGroup Sessions { get; set; }

        public ConcurrentQueue<IPacket> LastPackets { get; }
        public Dictionary<Type, Subject<RequestData<MapInstance>>> Requests { get; set; }
        public List<Task> HandlerTasks { get; set; } = new();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Kick()
        {
            Kick(o => o != null);
        }

        public void Kick(Func<ICharacterEntity, bool> filter)
        {
            _sessionRegistry.GetCharacters(filter)
                .Where(s => !s.IsDisconnecting && s.MapInstanceId == MapInstanceId).ToList()
                .ForEach(s => s.ChangeMapAsync(_mapChangeService, s.MapId, s.MapX, s.MapY));
        }

        public MapItem? PutItem(short amount, IItemInstance inv, ClientSession session)
        {
            var random2 = Guid.NewGuid();
            MapItem? droppedItem;
            var possibilities = new List<(short X, short Y)>();

            for (short x = -1; x < 2; x++)
            {
                for (short y = -1; y < 2; y++)
                {
                    possibilities.Add((x, y));
                }
            }

            short mapX = 0;
            short mapY = 0;
            var niceSpot = false;
            var orderedPossibilities = possibilities.OrderBy(_ => RandomHelper.Instance.RandomNumber()).ToList();
            for (var i = 0; (i < orderedPossibilities.Count) && !niceSpot; i++)
            {
                mapX = (short)(session.Character.PositionX + orderedPossibilities[i].X);
                mapY = (short)(session.Character.PositionY + orderedPossibilities[i].Y);
                if (Map.IsBlockedZone(session.Character.PositionX, session.Character.PositionY, mapX, mapY))
                {
                    continue;
                }

                niceSpot = true;
            }

            if (!niceSpot)
            {
                return null;
            }

            if ((amount <= 0) || (amount > inv.Amount))
            {
                return null;
            }

            var newItemInstance = (IItemInstance)inv.Clone();
            newItemInstance.Id = random2;
            newItemInstance.Amount = amount;
            droppedItem = _mapItemGenerationService.Create(this, newItemInstance, mapX, mapY);
            MapItems[droppedItem.VisualId] = droppedItem;
            inv.Amount -= amount;
            if (inv.Amount == 0)
            {
                session.Character.InventoryService.DeleteById(inv.Id);
            }

            return droppedItem;
        }

        public void LoadMonsters(List<MapMonster> monsters)
        {
            _monsters = new ConcurrentDictionary<int, MapMonster>(monsters.ToDictionary(x => x.MapMonsterId,
                x =>
                {
                    x.MapInstanceId = MapInstanceId;
                    x.MapInstance = this;
                    return x;
                }));
        }

        public void LoadNpcs(List<MapNpc> npcs)
        {
            _npcs = new ConcurrentDictionary<int, MapNpc>(npcs.ToDictionary(
                x => x.MapNpcId,
                x =>
                {
                    x.MapInstanceId = MapInstanceId;
                    x.MapInstance = this;
                    return x;
                }));
        }

        public List<IPacket> GetMapItems(RegionType language)
        {
            var packets = new List<IPacket>();
            // TODO: Parallelize getting of items of mapinstance
            Portals.ForEach(s => packets.Add(s.GenerateGp()));

            Monsters.ForEach(s =>
            {
                packets.Add(s.GenerateIn());

                if (s.Size != 10)
                {
                    packets.Add(s.GenerateCharSc());
                }
            });

            Npcs.ForEach(s =>
            {
                packets.Add(s.GenerateIn());

                if (s.Shop != null)
                {
                    var shopPacket = s.GenerateShop(language);
                    packets.Add(shopPacket);
                }

                if (s.Size != 10)
                {
                    packets.Add(s.GenerateCharSc());
                }
            });
            MapItems.Values.ToList().ForEach(s => packets.Add(s.GenerateIn()));
            return packets;
        }

        public CMapPacket GenerateCMap(bool enter)
        {
            return new CMapPacket
            {
                Type = 0,
                Id = Map.MapId,
                MapType = enter
            };
        }

        public Task StartLifeAsync()
        {
            async Task LifeAsync()
            {
                try
                {
                    if (IsSleeping)
                    {
                        return;
                    }

                    await Task.WhenAll(Monsters.Where(s => s.Life == null).Select(monster => monster.StartLifeAsync())).ConfigureAwait(false);
                    await Task.WhenAll(Npcs.Where(s => s.Life == null).Select(npc => npc.StartLifeAsync())).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e);
                }
            }
            Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Select(_ => LifeAsync()).Subscribe();
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || (Life == null))
            {
                return;
            }

            Life.Dispose();
            Life = null;
        }
    }
}