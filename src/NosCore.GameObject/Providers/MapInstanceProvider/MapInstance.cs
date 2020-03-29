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
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels.Groups;
using NosCore.Core;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PathFinder;
using Serilog;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapInstance : IBroadcastable, IDisposable
    {
        public short MaxPacketsBuffer { get; } = 250;
        private readonly ILogger _logger;

        private readonly List<IMapInstanceEventHandler> _mapInstanceEventHandler;
        private readonly IMapItemProvider _mapItemProvider;
        private bool _isSleeping;
        private bool _isSleepingRequest;
        private ConcurrentDictionary<int, MapMonster> _monsters;

        private ConcurrentDictionary<int, MapNpc> _npcs;

        public ConcurrentDictionary<Guid, MapDesignObject> MapDesignObjects =
            new ConcurrentDictionary<Guid, MapDesignObject>();

        public MapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType type,
            IMapItemProvider mapItemProvider, ILogger logger, List<IMapInstanceEventHandler> mapInstanceEventHandler)
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
            LastUnregister = SystemTime.Now().AddMinutes(-1);
            ExecutionEnvironment.TryGetCurrentExecutor(out var executor);
            Sessions = new DefaultChannelGroup(executor);
            _mapItemProvider = mapItemProvider;
            _logger = logger;
            Requests = new Dictionary<MapInstanceEventType, Subject<RequestData<MapInstance>>>();
            _mapInstanceEventHandler = mapInstanceEventHandler;
            foreach (MapInstanceEventType eventTypes in Enum.GetValues(typeof(MapInstanceEventType)))
            {
                Requests[eventTypes] = new Subject<RequestData<MapInstance>>();
            }
        }

        public DateTime LastUnregister { get; set; }

        public ConcurrentDictionary<long, MapItem> MapItems { get; }

        public bool IsSleeping
        {
            get
            {
                if (!_isSleepingRequest || _isSleeping || (LastUnregister.AddSeconds(30) >= SystemTime.Now()))
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
        public Dictionary<MapInstanceEventType, Subject<RequestData<MapInstance>>> Requests { get; set; }

        public IChannelGroup Sessions { get; set; }

        public ConcurrentQueue<IPacket> LastPackets { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void LoadHandlers()
        {
            _mapInstanceEventHandler.ForEach(handler =>
            {
                var type = handler.MapInstanceEventType;
                Requests[type].Subscribe(handler.Execute);
            });
        }

        public void Kick()
        {
            Kick(o => o != null);
        }

        public void Kick(Func<ICharacterEntity, bool> filter)
        {
            Broadcaster.Instance.GetCharacters(filter)
                .Where(s => !s.IsDisconnecting && s.MapInstanceId == MapInstanceId).ToList()
                .ForEach(s => s.ChangeMap(s.MapId, s.MapX, s.MapY));
        }

        public MapItem? PutItem(short amount, IItemInstance inv, ClientSession session)
        {
            var random2 = Guid.NewGuid();
            MapItem? droppedItem = null;
            var possibilities = new List<MapCell>();

            for (short x = -1; x < 2; x++)
            {
                for (short y = -1; y < 2; y++)
                {
                    possibilities.Add(new MapCell {X = x, Y = y});
                }
            }

            short mapX = 0;
            short mapY = 0;
            var niceSpot = false;
            var orderedPossibilities = possibilities.OrderBy(_ => RandomFactory.Instance.RandomNumber()).ToList();
            for (var i = 0; (i < orderedPossibilities.Count) && !niceSpot; i++)
            {
                mapX = (short) (session.Character.PositionX + orderedPossibilities[i].X);
                mapY = (short) (session.Character.PositionY + orderedPossibilities[i].Y);
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

            var newItemInstance = (IItemInstance) inv.Clone();
            newItemInstance.Id = random2;
            newItemInstance.Amount = amount;
            droppedItem = _mapItemProvider.Create(this, newItemInstance, mapX, mapY);
            MapItems[droppedItem.VisualId] = droppedItem;
            inv.Amount -= amount;
            if (inv.Amount == 0)
            {
                session.Character.InventoryService!.DeleteById(inv.Id);
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

        public List<IPacket> GetMapItems()
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
                    packets.Add(s.GenerateShop());
                }

                if (s.Size != 10)
                {
                    packets.Add(s.GenerateCharSc());
                }
            });
            MapItems.Values.ToList().ForEach(s => packets.Add(s.GenerateIn()));
            return packets;
        }

        public CMapPacket GenerateCMap()
        {
            return new CMapPacket
            {
                Type = 0,
                Id = Map.MapId,
                MapType = MapInstanceType != MapInstanceType.BaseMapInstance
            };
        }

        public void StartLife()
        {
            Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(_ =>
            {
                try
                {
                    if (IsSleeping)
                    {
                        return;
                    }

                    Parallel.ForEach(Monsters.Where(s => s.Life == null), monster => monster.StartLife());
                    Parallel.ForEach(Npcs.Where(s => s.Life == null), npc => npc.StartLife());
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e);
                }
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Life != null)
                {
                    Life.Dispose();
                    Life = null;
                }
            }
        }
    }
}