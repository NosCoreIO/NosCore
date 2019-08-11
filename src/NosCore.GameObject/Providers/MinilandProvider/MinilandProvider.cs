using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using NosCore.Core;
using NosCore.Data;
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
        private readonly ConcurrentDictionary<long, Miniland> _minilandIds;
        private readonly IGenericDao<MinilandDto> _minilandDao;
        private readonly IMapInstanceProvider _mapInstanceProvider;
        private readonly List<MapDto> _maps;
        private readonly IMapItemProvider _mapItemProvider;
        private readonly ILogger _logger;

        public MinilandProvider(IMapInstanceProvider mapInstanceProvider, List<MapDto> maps, ILogger logger, IMapItemProvider mapItemProvider, IGenericDao<MinilandDto> minilandDao)
        {
            _mapInstanceProvider = mapInstanceProvider;
            _maps = maps;
            _mapItemProvider = mapItemProvider;
            _logger = logger;
            _minilandIds = new ConcurrentDictionary<long, Miniland>();
            _minilandDao = minilandDao;
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
        public Miniland GetMiniland(long characterId)
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
                _minilandIds.TryRemove(characterId, out _);
            }
        }

        public Miniland Initialize(Character character)
        {
            var minilandInfoDto = _minilandDao.FirstOrDefault(s=>s.OwnerId == character.CharacterId);
            if(minilandInfoDto == null)
            {
                throw new ArgumentException();
            }

            var map = _maps.FirstOrDefault(s => s.MapId == 20001);
            var miniland = new MapInstance(map.Adapt<Map.Map>(), Guid.NewGuid(), map.ShopAllowed, MapInstanceType.NormalInstance,
                _mapItemProvider, _logger);

            var minilandInfo = minilandInfoDto.Adapt<Miniland>();
            minilandInfo.MapInstanceId = miniland.MapInstanceId;
            minilandInfo.Owner = character;

            _minilandIds.TryAdd(character.CharacterId, minilandInfo);
            _mapInstanceProvider.AddMapInstance(miniland);
            return minilandInfo;
        }

        public Miniland GetMinilandFromMapInstanceId(Guid mapInstanceId)
        {
           return _minilandIds.FirstOrDefault(s => s.Value.MapInstanceId == mapInstanceId).Value;
        }
    }
}
