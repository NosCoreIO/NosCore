using NosCore.Data;
using NosCore.Shared.Map;
using NosCore.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets;
using NosCore.GameObject.Networking;
using System.Threading.Tasks;
using NosCore.DAL;
using NosCore.Core.Serializing;

namespace NosCore.GameObject
{
    public class MapInstance : BroadcastableBase
    {
        #region Instantiation

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

        #endregion

        #region Members

        private readonly ConcurrentDictionary<long, MapMonsterDTO> _monsters;

        private readonly ConcurrentDictionary<long, MapNpcDTO> _npcs;

        #endregion

        #region Properties

        public int DropRate { get; set; }

        public bool IsDancing { get; set; }

        public bool IsPVP { get; set; }

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

        public CMapPacket GenerateCMap()
        {
            return new CMapPacket()
            {
                Type = 0,
                Id = Map.MapId,
                MapType = MapInstanceType != MapInstanceType.BaseMapInstance
            };
        }
        #endregion

        public void LoadPortals()
        {
            OrderablePartitioner<PortalDTO> partitioner = Partitioner.Create(DAOFactory.PortalDAO.Where(s => s.SourceMapId.Equals(Map.MapId)),
                EnumerablePartitionerOptions.None);
            ConcurrentDictionary<int, Portal> portalList = new ConcurrentDictionary<int, Portal>();
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
            List<PacketDefinition> packets = new List<PacketDefinition>();
            // TODO: Parallelize getting of items of mapinstance
            Portals.ForEach(s => packets.Add(s.GenerateGp()));
            return packets;
        }
    }
}