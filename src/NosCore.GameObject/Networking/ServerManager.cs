using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
	public class ServerManager : BroadcastableBase
	{
		private static ServerManager instance;

		private static readonly ConcurrentDictionary<Guid, MapInstance> _mapinstances =
			new ConcurrentDictionary<Guid, MapInstance>();

		private static readonly List<Map.Map> _maps = new List<Map.Map>();

		private ServerManager()
		{
		}

		public static ServerManager Instance => instance ?? (instance = new ServerManager());

		public MapInstance GenerateMapInstance(short mapId, MapInstanceType type)
		{
			var map = _maps.Find(m => m.MapId.Equals(mapId));
			if (map == null)
			{
				return null;
			}

			var guid = Guid.NewGuid();
			var mapInstance = new MapInstance(map, guid, false, type);
			mapInstance.LoadPortals();
			_mapinstances.TryAdd(guid, mapInstance);
			return mapInstance;
		}

		public void Initialize()
		{
			// parse rates
			try
			{
				var i = 0;
				var monstercount = 0;
				var mapPartitioner = Partitioner.Create(DAOFactory.MapDAO.LoadAll().Cast<Map.Map>(),
					EnumerablePartitionerOptions.NoBuffering);
				var _mapList = new ConcurrentDictionary<short, Map.Map>();
				Parallel.ForEach(mapPartitioner, new ParallelOptions {MaxDegreeOfParallelism = 8}, map =>
				{
					var guid = Guid.NewGuid();
					map.Initialize();
					_mapList[map.MapId] = map;
					var newMap = new MapInstance(map, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance);
					_mapinstances.TryAdd(guid, newMap);
					Task.Run(() => newMap.LoadPortals());
					monstercount += newMap.Monsters.Count;
					i++;
				});
				_maps.AddRange(_mapList.Select(s => s.Value));
				if (i != 0)
				{
					Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPS_LOADED), i));
				}
				else
				{
					Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NO_MAP));
				}

				Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPMONSTERS_LOADED),
					monstercount));
			}
			catch (Exception ex)
			{
				Logger.Log.Error("General Error", ex);
			}
		}

		public Guid GetBaseMapInstanceIdByMapId(short MapId)
		{
			return _mapinstances.FirstOrDefault(s =>
				s.Value?.Map.MapId == MapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
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
			Sessions.TryRemove(clientSession.SessionId, out var clientSessionuseless);
		}
	}
}