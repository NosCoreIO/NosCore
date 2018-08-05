using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Serializing;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using System.Reactive.Linq;
using NosCore.GameObject.Item;
using NosCore.PathFinder;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject
{
    public class MapInstance : BroadcastableBase
    {
        private readonly ConcurrentDictionary<long, MapMonster> _monsters;

        private readonly ConcurrentDictionary<long, MapNpc> _npcs;

        public ConcurrentDictionary<long, MapItem> DroppedList { get; }

        public MapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType type)
        {
            XpRate = 1;
            DropRate = 1;
            ShopAllowed = shopAllowed;
            MapInstanceType = type;
            Map = map;
            MapInstanceId = guid;
            Portals = new List<Portal>();
            _monsters = new ConcurrentDictionary<long, MapMonster>();
            _npcs = new ConcurrentDictionary<long, MapNpc>();
            DroppedList = new ConcurrentDictionary<long, MapItem>();
            _isSleeping = true;
        }

        private bool _isSleeping;
        private bool _isSleepingRequest;
        public bool IsSleeping
        {
            get
            {
                if (!_isSleepingRequest || _isSleeping || LastUnregister.AddSeconds(30) >= DateTime.Now)
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

        public MapItem PutItem(PocketType type, short slot, short amount, ref ItemInstance inv, ClientSession session)
        {
            Guid random2 = Guid.NewGuid();
            MapItem droppedItem = null;
            List<MapCell> possibilities = new List<MapCell>();

            for (short x = -2; x < 3; x++)
            {
                for (short y = -2; y < 3; y++)
                {
                    possibilities.Add(new MapCell { X = x, Y = y });
                }
            }

            short mapX = 0;
            short mapY = 0;
            var niceSpot = false;
            foreach (MapCell possibility in possibilities.OrderBy(s => ServerManager.Instance.RandomNumber()))
            {
                mapX = (short)(session.Character.PositionX + possibility.X);
                mapY = (short)(session.Character.PositionY + possibility.Y);
                if (!Map.IsWalkable(Map[mapX, mapY]))
                {
                    continue;
                }
                niceSpot = true;
                break;
            }

            if (!niceSpot)
            {
                return null;
            }
            if (amount <= 0 || amount > inv.Amount)
            {
                return null;
            }
            var newItemInstance = inv.Clone();
            newItemInstance.Id = random2;
            newItemInstance.Amount = amount;
            droppedItem = new MapItem{MapInstance = this, VNum = newItemInstance.ItemVNum, PositionX = mapX,PositionY = mapY, Amount = amount};
            DroppedList[droppedItem.VisualId] = droppedItem;
            inv.Amount -= amount;
            return droppedItem;
        }

        public Map.Map Map { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType MapInstanceType { get; set; }

        internal void LoadMonsters()
        {
            var partitioner = Partitioner.Create(DAOFactory.MapMonsterDAO.Where(s => s.MapId == Map.MapId), EnumerablePartitionerOptions.None);
            Parallel.ForEach(partitioner, monster =>
            {
                if (!(monster is MapMonster mapMonster))
                {
                    return;
                }
                mapMonster.Initialize();
                mapMonster.MapInstance = this;
                mapMonster.MapInstanceId = MapInstanceId;
                _monsters[mapMonster.MapMonsterId] = mapMonster;
            });
        }

        internal void LoadNpcs()
        {
            var partitioner = Partitioner.Create(DAOFactory.MapNpcDAO.Where(s => s.MapId == Map.MapId), EnumerablePartitionerOptions.None);
            Parallel.ForEach(partitioner, npc =>
            {
                if (!(npc is MapNpc mapNpc))
                {
                    return;
                }
                mapNpc.Initialize();
                mapNpc.MapInstance = this;
                mapNpc.MapInstanceId = MapInstanceId;
                _npcs[mapNpc.MapNpcId] = mapNpc;
            });
        }

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

        public void LoadPortals()
        {
            var partitioner = Partitioner.Create(DAOFactory.PortalDAO.Where(s => s.SourceMapId.Equals(Map.MapId)),
                EnumerablePartitionerOptions.None);
            var portalList = new ConcurrentDictionary<int, Portal>();
            Parallel.ForEach(partitioner, portal =>
            {
                if (!(portal is Portal portal2))
                {
                    return;
                }

                portal2.SourceMapInstanceId = MapInstanceId;
                portalList[portal2.PortalId] = portal2;
            });
            Portals.AddRange(portalList.Select(s => s.Value));
        }

        public List<PacketDefinition> GetMapItems()
        {
            var packets = new List<PacketDefinition>();
            // TODO: Parallelize getting of items of mapinstance
            Portals.ForEach(s => packets.Add(s.GenerateGp()));
            Monsters.ForEach(s =>
            {
                packets.Add(s.GenerateIn());
            });
            Npcs.ForEach(s =>
            {
                packets.Add(s.GenerateIn());
            });
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
                       Parallel.ForEach(Npcs.Where(s=>s.Life == null), npc => npc.StartLife());
                   }
                   catch (Exception e)
                   {
                       Logger.Log.Error(e);
                   }
               });
        }


        private IDisposable Life { get; set; }
    }
}