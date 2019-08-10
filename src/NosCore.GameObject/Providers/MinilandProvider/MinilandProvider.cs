using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using Serilog;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public class MinilandProvider : IMinilandProvider
    {
        private readonly ConcurrentDictionary<long, MinilandInfo> _minilandIds;
        private readonly IMapInstanceProvider _mapInstanceProvider;
        private readonly List<MapDto> _maps;
        private readonly IMapItemProvider _mapItemProvider;
        private readonly ILogger _logger;

        public MinilandProvider(IMapInstanceProvider mapInstanceProvider, List<MapDto> maps, ILogger logger, IMapItemProvider mapItemProvider)
        {
            _mapInstanceProvider = mapInstanceProvider;
            _maps = maps;
            _mapItemProvider = mapItemProvider;
            _logger = logger;
            _minilandIds = new ConcurrentDictionary<long, MinilandInfo>();
        }

        public List<Portal> GetMinilandPortals(long characterId)
        {
            var nosville = _mapInstanceProvider.GetBaseMapById(1);
            var oldNosville = _mapInstanceProvider.GetBaseMapById(145);
            var miniland = _mapInstanceProvider.GetMapInstance(_minilandIds[characterId].MapInstanceId);
            return new List<Portal> { new Portal
            {
                SourceX = 48,
                SourceY = 132,
                DestinationX = 5,
                DestinationY = 8,
                Type = PortalType.Miniland,
                SourceMapId = 1,
                DestinationMapId = 20001,
                DestinationMapInstanceId = miniland.MapInstanceId,
                SourceMapInstanceId = nosville.MapInstanceId
            }, new Portal
            {
                SourceX = 9,
                SourceY = 171,
                DestinationX = 5,
                DestinationY = 8,
                Type = PortalType.Miniland,
                SourceMapId = 145,
                DestinationMapId = 20001,
                DestinationMapInstanceId = miniland.MapInstanceId,
                SourceMapInstanceId = oldNosville.MapInstanceId
            } };

        }
        public MinilandInfo GetMinilandInfo(long characterId)
        {
            if (_minilandIds.ContainsKey(characterId))
            {
                return _minilandIds[characterId];
            }
            throw new ArgumentException();
        }

        public void DeleteMiniland(long characterId)
        {
            if (_minilandIds.ContainsKey(characterId))
            {
                _mapInstanceProvider.RemoveMap(_minilandIds[characterId].MapInstanceId);
            }
        }

        public MinilandInfo Initialize(long characterId, MinilandState state)
        {
            var map = _maps.FirstOrDefault(s => s.MapId == 20001);
            var miniland = new MapInstance(map.Adapt<Map.Map>(), Guid.NewGuid(), map.ShopAllowed, MapInstanceType.NormalInstance,
                _mapItemProvider, _logger);
            var minilandInfo = new MinilandInfo
            {
                MapInstanceId = miniland.MapInstanceId,
                State = state
            };
            _minilandIds.TryAdd(characterId, minilandInfo);
            _mapInstanceProvider.AddMapInstance(miniland);
            return minilandInfo;
        }
    }
}
