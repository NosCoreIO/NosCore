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
	public sealed class ServerManager : BroadcastableBase
	{
		private static ServerManager _instance;

		private static readonly ConcurrentDictionary<Guid, MapInstance> Mapinstances =
			new ConcurrentDictionary<Guid, MapInstance>();

		private static readonly List<Map.Map> Maps = new List<Map.Map>();

		private ServerManager()
		{
		}

		public static ServerManager Instance => _instance ?? (_instance = new ServerManager());

		public MapInstance GenerateMapInstance(short mapId, MapInstanceType type)
		{
			var map = Maps.Find(m => m.MapId.Equals(mapId));
			if (map == null)
			{
				return null;
			}

			var guid = Guid.NewGuid();
			var mapInstance = new MapInstance(map, guid, false, type);
			mapInstance.LoadPortals();
			Mapinstances.TryAdd(guid, mapInstance);
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
				var mapList = new ConcurrentDictionary<short, Map.Map>();
				Parallel.ForEach(mapPartitioner, new ParallelOptions {MaxDegreeOfParallelism = 8}, map =>
				{
					var guid = Guid.NewGuid();
					map.Initialize();
					mapList[map.MapId] = map;
					var newMap = new MapInstance(map, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance);
					Mapinstances.TryAdd(guid, newMap);
					Task.Run(() => newMap.LoadPortals());
					monstercount += newMap.Monsters.Count;
					i++;
				});
				Maps.AddRange(mapList.Select(s => s.Value));
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

		public Guid GetBaseMapInstanceIdByMapId(short mapId)
		{
			return Mapinstances.FirstOrDefault(s =>
				s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
		}

		public MapInstance GetMapInstance(Guid id)
		{
			return Mapinstances.ContainsKey(id) ? Mapinstances[id] : null;
		}

		internal void RegisterSession(ClientSession clientSession)
		{
			Sessions.TryAdd(clientSession.SessionId, clientSession);
		}

		internal void UnregisterSession(ClientSession clientSession)
		{
			Sessions.TryRemove(clientSession.SessionId, out _);
		}
	}
}