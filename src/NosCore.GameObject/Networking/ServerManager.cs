using NosCore.Core.Logger;
using NosCore.DAL;
using NosCore.Data;
using NosCore.Domain.Map;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject
{
    public class ServerManager
    {
        private static ServerManager instance;

        private ServerManager() { }

        public static ServerManager Instance
        {
            get
            {
                return instance ?? (instance = new ServerManager());
            }
        }
        public ConcurrentDictionary<int, ClientSession> Sessions { get; set; } = new ConcurrentDictionary<int, ClientSession>();

        private static readonly ConcurrentDictionary<Guid, MapInstance> _mapinstances = new ConcurrentDictionary<Guid, MapInstance>();

        private static readonly List<MapDTO> _maps = new List<MapDTO>();

        public MapInstance GenerateMapInstance(short mapId, MapInstanceType type)
        {
            MapDTO map = _maps.Find(m => m.MapId.Equals(mapId));
            if (map == null)
            {
                return null;
            }
            Guid guid = Guid.NewGuid();
            MapInstance mapInstance = new MapInstance(map, guid, false, type);
            _mapinstances.TryAdd(guid, mapInstance);
            return mapInstance;
        }

        public void Initialize()
        {
            // parse rates
            try
            {
                int i = 0;
                int monstercount = 0;
                OrderablePartitioner<MapDTO> mapPartitioner = Partitioner.Create(DAOFactory.MapDAO.LoadAll(), EnumerablePartitionerOptions.NoBuffering);
                ConcurrentDictionary<short, MapDTO> _mapList = new ConcurrentDictionary<short, MapDTO>();
                Parallel.ForEach(mapPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, map =>
                {
                    Guid guid = Guid.NewGuid();
                    MapDTO mapinfo = new MapDTO()
                    {

                        Music = map.Music,
                        Data = map.Data,
                        MapId = map.MapId
                    };
                    _mapList[map.MapId] = mapinfo;
                    MapInstance newMap = new MapInstance(mapinfo, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance);
                    _mapinstances.TryAdd(guid, newMap);

                    monstercount += newMap.Monsters.Count;
                    i++;
                });
                _maps.AddRange(_mapList.Select(s => s.Value));
                if (i != 0)
                {
                    Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("MAPS_LOADED"), i));
                }
                else
                {
                    Logger.Log.Error(Language.Instance.GetMessageFromKey("NO_MAP"));
                }
                Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("MAPMONSTERS_LOADED"), monstercount));
            }
            catch (Exception ex)
            {
                Logger.Log.Error("General Error", ex);
            }
        }

        public Guid GetBaseMapInstanceIdByMapId(short MapId)
        {
            return _mapinstances.FirstOrDefault(s => s.Value?.Map.MapId == MapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
        }

        public MapInstance GetMapInstance(Guid id)
        {
            return _mapinstances.ContainsKey(id) ? _mapinstances[id] : null;
        }

        internal void RegisterSession(ClientSession clientSession)
        {
            Sessions.TryAdd(clientSession.SessionId, clientSession);
        }

        internal void UnregisterSession(ClientSession clientSession)
        {
           Sessions.TryRemove(clientSession.SessionId, out ClientSession clientSessionuseless);
        }
    }
}
