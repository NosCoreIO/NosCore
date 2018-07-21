using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NosCore.Core.Serializing;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.GameObject
{
    public class MapInstance : BroadcastableBase
    {
        private readonly ConcurrentDictionary<long, MapMonsterDTO> _monsters;

        private readonly ConcurrentDictionary<long, MapNpcDTO> _npcs;

        public MapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType type)
        {
            XpRate = 1;
            DropRate = 1;
            ShopAllowed = shopAllowed;
            MapInstanceType = type;
            Map = map;
            MapInstanceId = guid;
            Portals = new List<Portal>();
            _monsters = new ConcurrentDictionary<long, MapMonsterDTO>();
            _npcs = new ConcurrentDictionary<long, MapNpcDTO>();
        }

        public int DropRate { get; set; }

        public Map.Map Map { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType MapInstanceType { get; set; }

        public List<MapMonsterDTO> Monsters
        {
            get { return _monsters.Select(s => s.Value).ToList(); }
        }

        public List<MapNpcDTO> Npcs
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