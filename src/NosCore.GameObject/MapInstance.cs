using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NosCore.Core.Serializing;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.GameObject
{
    public class MapInstance : BroadcastableBase
    {
        private readonly ConcurrentDictionary<long, MapMonster> _monsters;

        private readonly ConcurrentDictionary<long, MapNpc> _npcs;

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
        }

        public int DropRate { get; set; }

        public Map.Map Map { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType MapInstanceType { get; set; }

        internal void LoadMonsters()
        {
            OrderablePartitioner<MapMonsterDTO> partitioner = Partitioner.Create(DAOFactory.MapMonsterDAO.Where(s => s.MapId == Map.MapId), EnumerablePartitionerOptions.None);
            Parallel.ForEach(partitioner, monster =>
            {
                if (!(monster is MapMonster mapMonster))
                {
                    return;
                }
                mapMonster.Initialize(this);
                _monsters[mapMonster.MapMonsterId] = mapMonster;
            });
        }

        internal void LoadNpcs()
        {
            OrderablePartitioner<MapNpcDTO> partitioner = Partitioner.Create(DAOFactory.MapNpcDAO.Where(s => s.MapId == Map.MapId), EnumerablePartitionerOptions.None);
            Parallel.ForEach(partitioner, npc =>
            {
                if (!(npc is MapNpc mapNpc))
                {
                    return;
                }
                mapNpc.Initialize(this);
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

        public void StartLife()
        {
            _life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(x =>
            {
                try
                {
                    if (!IsSleeping)
                    {
                        Monsters.ForEach(s => s.StartLife());
                        Npcs.ForEach(s => s.StartLife());
                    }
                }
                catch (Exception e)
                {
                    Logger.Log.Error(e);
                }
            });
        }

        public bool IsSleeping { get; set; }

        private IDisposable _life { get; set; }

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
    }
}