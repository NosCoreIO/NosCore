//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public class MinilandProvider : IMinilandProvider
    {
        private readonly IMapInstanceProvider _mapInstanceProvider;
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly List<MapDto> _maps;
        private readonly IDao<MinilandDto, Guid> _minilandDao;
        private readonly ConcurrentDictionary<long, Miniland> _minilandIds;
        private readonly IDao<MinilandObjectDto, Guid> _minilandObjectsDao;

        public MinilandProvider(IMapInstanceProvider mapInstanceProvider, IFriendHttpClient friendHttpClient, List<MapDto> maps,
            IDao<MinilandDto, Guid> minilandDao, IDao<MinilandObjectDto, Guid> minilandObjectsDao)
        {
            _mapInstanceProvider = mapInstanceProvider;
            _friendHttpClient = friendHttpClient;
            _maps = maps;
            _minilandIds = new ConcurrentDictionary<long, Miniland>();
            _minilandDao = minilandDao;
            _minilandObjectsDao = minilandObjectsDao;
        }

        public List<Portal> GetMinilandPortals(long characterId)
        {
            var nosville = _mapInstanceProvider.GetBaseMapById(1);
            var oldNosville = _mapInstanceProvider.GetBaseMapById(145);
            var miniland = _mapInstanceProvider.GetMapInstance(_minilandIds[characterId].MapInstanceId);
            return new List<Portal>
            {
                new Portal
                {
                    SourceX = 48,
                    SourceY = 132,
                    DestinationX = 5,
                    DestinationY = 8,
                    Type = PortalType.Miniland,
                    SourceMapId = 1,
                    DestinationMapId = 20001,
                    DestinationMapInstanceId = miniland!.MapInstanceId,
                    SourceMapInstanceId = nosville!.MapInstanceId
                },
                new Portal
                {
                    SourceX = 9,
                    SourceY = 171,
                    DestinationX = 5,
                    DestinationY = 8,
                    Type = PortalType.Miniland,
                    SourceMapId = 145,
                    DestinationMapId = 20001,
                    DestinationMapInstanceId = miniland.MapInstanceId,
                    SourceMapInstanceId = oldNosville!.MapInstanceId
                }
            };
        }

        public Miniland GetMiniland(long characterId)
        {
            if (_minilandIds.ContainsKey(characterId))
            {
                return _minilandIds[characterId];
            }

            throw new ArgumentException();
        }

        public async Task DeleteMinilandAsync(long characterId)
        {
            if (!_minilandIds.ContainsKey(characterId))
            {
                return;
            }

            var miniland = _mapInstanceProvider.GetMapInstance(_minilandIds[characterId].MapInstanceId);
            foreach (var obj in miniland!.MapDesignObjects.Values)
            {
                await _minilandObjectsDao.TryInsertOrUpdateAsync(obj).ConfigureAwait(false);
            }

            _mapInstanceProvider.RemoveMap(_minilandIds[characterId].MapInstanceId);
            _minilandIds.TryRemove(characterId, out _);
        }

        public async Task<Miniland> InitializeAsync(Character character)
        {
            var minilandInfoDto = await _minilandDao.FirstOrDefaultAsync(s => s.OwnerId == character.CharacterId).ConfigureAwait(false);
            if (minilandInfoDto == null)
            {
                throw new ArgumentException();
            }

            var map = _maps.First(s => s.MapId == 20001);
            var miniland = _mapInstanceProvider.CreateMapInstance(map.Adapt<Map.Map>(), Guid.NewGuid(), map.ShopAllowed,
                MapInstanceType.NormalInstance, new List<IMapInstanceEventHandler> {new MinilandEntranceHandler(this)});

            var minilandInfo = minilandInfoDto.Adapt<Miniland>();
            minilandInfo.MapInstanceId = miniland.MapInstanceId;
            minilandInfo.CharacterEntity = character;

            _minilandIds.TryAdd(character.CharacterId, minilandInfo);
            await _mapInstanceProvider.AddMapInstanceAsync(miniland).ConfigureAwait(false);
            miniland.LoadHandlers();

            var listobjects = character.InventoryService.Values.Where(s => s.Type == NoscorePocketType.Miniland).ToArray();
            var idlist = listobjects.Select(s => s.Id).ToArray();
            var minilandObjectsDto = _minilandObjectsDao.Where(s => idlist.Contains((Guid) s.InventoryItemInstanceId!))
                .ToList();
            foreach (var mlobjdto in minilandObjectsDto)
            {
                var mlobj = mlobjdto.Adapt<MapDesignObject>();
                AddMinilandObject(mlobj, character.CharacterId,
                    listobjects.First(s => s.Id == mlobjdto.InventoryItemInstanceId));
            }

            return minilandInfo;
        }

        public async Task SetStateAsync(long characterId, MinilandState state)
        {
            if (!_minilandIds.ContainsKey(characterId))
            {
                throw new ArgumentException();
            }

            var ml = _minilandIds[characterId];
            var miniland = _mapInstanceProvider.GetMapInstance(ml.MapInstanceId);
            ml.State = state;

            switch (ml.State)
            {
                case MinilandState.Open:
                    return;
                case MinilandState.Private:
                {
                    List<long> friends = (await _friendHttpClient.GetListFriendsAsync(characterId).ConfigureAwait(false))
                        .Select(s => s.CharacterId)
                        .ToList();
                    // Kick all players in miniland except owner and his friends
                    miniland!.Kick(o => o.VisualId != characterId && !friends.Contains(o.VisualId));
                    break;
                }
                default:
                    miniland!.Kick(o => o.VisualId != characterId);
                    break;
            }
        }

        public Miniland GetMinilandFromMapInstanceId(Guid mapInstanceId)
        {
            return _minilandIds.FirstOrDefault(s => s.Value.MapInstanceId == mapInstanceId).Value;
        }

        public void AddMinilandObject(MapDesignObject mapObject, long characterId, InventoryItemInstance minilandobject)
        {
            var miniland = _mapInstanceProvider.GetMapInstance(_minilandIds[characterId].MapInstanceId);

            mapObject.Effect =
                (short) (minilandobject?.ItemInstance?.Item?.EffectValue ?? minilandobject?.ItemInstance?.Design ?? 0);
            mapObject.Width = minilandobject?.ItemInstance?.Item?.Width ?? 0;
            mapObject.Height = minilandobject?.ItemInstance?.Item?.Height ?? 0;
            mapObject.DurabilityPoint = (short)(minilandobject?.ItemInstance?.DurabilityPoint ?? 0);
            mapObject.IsWarehouse = minilandobject?.ItemInstance?.Item?.IsWarehouse ?? false;
            mapObject.InventoryItemInstanceId = minilandobject?.Id;
            mapObject.InventoryItemInstance = minilandobject;
            mapObject.Slot = minilandobject?.Slot ?? 0;

            if (minilandobject?.ItemInstance?.Item?.ItemType == ItemType.House)
            {
                switch (minilandobject.ItemInstance.Item.ItemSubType)
                {
                    case 0:
                        mapObject.MapX = 24;
                        mapObject.MapY = 7;
                        break;

                    case 1:
                        mapObject.MapX = 21;
                        mapObject.MapY = 4;
                        break;

                    case 2:
                        mapObject.MapX = 31;
                        mapObject.MapY = 3;
                        break;
                }
            }

            miniland!.MapDesignObjects.TryAdd(minilandobject!.Id, mapObject);
        }
    }
}