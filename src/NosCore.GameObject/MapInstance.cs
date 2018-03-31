using NosCore.Data;
using NosCore.Domain.Map;
using NosCore.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject
{
    public class MapInstance
    {
        #region Instantiation

        public MapInstance(MapDTO map, Guid guid, bool shopAllowed, MapInstanceType type)
        {
            XpRate = 1;
            DropRate = 1;
            ShopAllowed = shopAllowed;
            MapInstanceType = type;
            Map = map;
            MapInstanceId = guid;
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

        public MapDTO Map { get; set; }

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

    }
}