//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels.Groups;
using NosCore.Core;
using NosCore.Core.Serializing;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapMonsterProvider;
using NosCore.GameObject.Providers.MapNpcProvider;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapInstance : IBroadcastable
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private readonly IMapItemProvider _mapItemProvider;
        private readonly IMapMonsterProvider _mapMonsterProvider;
        private readonly IMapNpcProvider _mapNpcProvider;

        private readonly List<NpcMonsterDto> _npcMonsters;

        private bool _isSleeping;
        private bool _isSleepingRequest;
        private ConcurrentDictionary<long, MapMonster> _monsters;

        private ConcurrentDictionary<long, MapNpc> _npcs;

        public MapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType type,
            List<NpcMonsterDto> npcMonsters, IMapItemProvider mapItemProvider,
            IMapNpcProvider mapNpcProvider, IMapMonsterProvider mapMonsterProvider)
        {
            _npcMonsters = npcMonsters;
            XpRate = 1;
            DropRate = 1;
            ShopAllowed = shopAllowed;
            MapInstanceType = type;
            Map = map;
            MapInstanceId = guid;
            Portals = new List<Portal>();
            _monsters = new ConcurrentDictionary<long, MapMonster>();
            _npcs = new ConcurrentDictionary<long, MapNpc>();
            MapItems = new ConcurrentDictionary<long, MapItem>();
            _isSleeping = true;
            LastUnregister = SystemTime.Now().AddMinutes(-1);
            ExecutionEnvironment.TryGetCurrentExecutor(out var executor);
            Sessions = new DefaultChannelGroup(executor);
            _mapItemProvider = mapItemProvider;
            _mapNpcProvider = mapNpcProvider;
            _mapMonsterProvider = mapMonsterProvider;
        }

        public DateTime LastUnregister { get; set; }

        public ConcurrentDictionary<long, MapItem> MapItems { get; }

        public bool IsSleeping
        {
            get
            {
                if (!_isSleepingRequest || _isSleeping || LastUnregister.AddSeconds(30) >= SystemTime.Now())
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

        private IDisposable Life { get; set; }

        public IChannelGroup Sessions { get; set; }

        public MapItem PutItem(short amount, IItemInstance inv, ClientSession session)
        {
            Guid random2 = Guid.NewGuid();
            MapItem droppedItem = null;
            List<MapCell> possibilities = new List<MapCell>();

            for (short x = -2; x < 3; x++)
            {
                for (short y = -2; y < 3; y++)
                {
                    possibilities.Add(new MapCell {X = x, Y = y});
                }
            }

            short mapX = 0;
            short mapY = 0;
            var niceSpot = false;
            var orderedPossibilities = possibilities.OrderBy(_ => RandomFactory.Instance.RandomNumber()).ToList();
            for (var i = 0; i < orderedPossibilities.Count && !niceSpot; i++)
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

            if (amount <= 0 || amount > inv.Amount)
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
                session.Character.Inventory.DeleteById(inv.Id);
            }

            return droppedItem;
        }

        public void LoadMonsters()
        {
            _monsters = _mapMonsterProvider.Create(this);
        }

        public void LoadNpcs()
        {
            _npcs = _mapNpcProvider.Create(this);
        }

        public List<PacketDefinition> GetMapItems()
        {
            var packets = new List<PacketDefinition>();
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
    }
}