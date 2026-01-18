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

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;

namespace NosCore.GameObject.Services.MinilandService
{
    public class MinilandService(IMapInstanceAccessorService mapInstanceAccessorService,
            IFriendHub friendHttpClient, List<MapDto> maps,
            IDao<MinilandDto, Guid> minilandDao, IDao<MinilandObjectDto, Guid> minilandObjectsDao,
            IMinilandRegistry minilandRegistry)
        : IMinilandService
    {
        public Miniland GetMiniland(long characterId)
        {
            var miniland = minilandRegistry.GetMiniland(characterId);
            if (miniland != null)
            {
                return miniland;
            }

            throw new ArgumentException();
        }

        public async Task<Guid?> DeleteMinilandAsync(long characterId)
        {
            var ml = minilandRegistry.GetMiniland(characterId);
            if (ml == null)
            {
                return null;
            }

            var miniland = mapInstanceAccessorService.GetMapInstance(ml.MapInstanceId);
            foreach (var obj in miniland!.MapDesignObjects.Values)
            {
                await minilandObjectsDao.TryInsertOrUpdateAsync(obj).ConfigureAwait(false);
            }

            if (minilandRegistry.TryUnregister(characterId, out var mapInstance))
            {
                return mapInstance?.MapInstanceId;
            }

            return null;
        }

        public async Task<Miniland> InitializeAsync(PlayerContext player, IMapInstanceGeneratorService generator)
        {
            var minilandInfoDto = await minilandDao.FirstOrDefaultAsync(s => s.OwnerId == player.CharacterId).ConfigureAwait(false);
            if (minilandInfoDto == null)
            {
                throw new ArgumentException();
            }

            var map = maps.First(s => s.MapId == 20001);
            var miniland = generator.CreateMapInstance(map.Adapt<Map.Map>(), Guid.NewGuid(), map.ShopAllowed,
                MapInstanceType.NormalInstance);

            var minilandInfo = minilandInfoDto.Adapt<Miniland>();
            minilandInfo.MapInstanceId = miniland.MapInstanceId;
            minilandInfo.OwnerName = player.Name;

            var nosville = mapInstanceAccessorService.GetBaseMapById(1);
            var oldNosville = mapInstanceAccessorService.GetBaseMapById(145);

            if (nosville != null)
            {
                var nosvillePortalEntity = nosville.EcsWorld.CreatePortal(
                    0, 48, 132, 1, 5, 8, 20001, PortalType.Miniland, false,
                    nosville.MapInstanceId, miniland.MapInstanceId, player.CharacterId);
                nosville.Portals.Add(nosvillePortalEntity);
            }

            if (oldNosville != null)
            {
                var oldNosvillePortalEntity = oldNosville.EcsWorld.CreatePortal(
                    0, 9, 171, 145, 5, 8, 20001, PortalType.Miniland, false,
                    oldNosville.MapInstanceId, miniland.MapInstanceId, player.CharacterId);
                oldNosville.Portals.Add(oldNosvillePortalEntity);
            }

            minilandRegistry.TryRegister(player.CharacterId, minilandInfo);
            await generator.AddMapInstanceAsync(miniland).ConfigureAwait(false);

            var listobjects = player.InventoryService.Values.Where(s => s.Type == NoscorePocketType.Miniland).ToArray();
            var idlist = listobjects.Select(s => s.Id).ToList();
            var minilandObjectsDto = minilandObjectsDao.Where(s => idlist.Contains((Guid)s.InventoryItemInstanceId!))?
                .ToList() ?? new List<MinilandObjectDto>();
            foreach (var mlobjdto in minilandObjectsDto)
            {
                var mlobj = mlobjdto.Adapt<MapDesignObject>();
                AddMinilandObject(mlobj, player.CharacterId,
                    listobjects.First(s => s.Id == mlobjdto.InventoryItemInstanceId));
            }

            return minilandInfo;
        }

        public async Task SetStateAsync(long characterId, MinilandState state)
        {
            var ml = minilandRegistry.GetMiniland(characterId);
            if (ml == null)
            {
                throw new ArgumentException();
            }

            var miniland = mapInstanceAccessorService.GetMapInstance(ml.MapInstanceId);
            ml.State = state;

            switch (ml.State)
            {
                case MinilandState.Open:
                    return;
                case MinilandState.Private:
                    {
                        List<long> friends = (await friendHttpClient.GetFriendsAsync(characterId).ConfigureAwait(false))
                            .Select(s => s.CharacterId)
                            .ToList();
                        await miniland!.KickAsync(o => o.VisualId != characterId && !friends.Contains(o.VisualId)).ConfigureAwait(false);
                        break;
                    }
                default:
                    await miniland!.KickAsync(o => o.VisualId != characterId).ConfigureAwait(false);
                    break;
            }
        }

        public Miniland? GetMinilandFromMapInstanceId(Guid mapInstanceId)
        {
            return minilandRegistry.FindByMapInstanceId(mapInstanceId);
        }

        public void AddMinilandObject(MapDesignObject mapObject, long characterId, InventoryItemInstance minilandobject)
        {
            var ml = minilandRegistry.GetMiniland(characterId);
            if (ml == null)
            {
                throw new ArgumentException();
            }
            var miniland = mapInstanceAccessorService.GetMapInstance(ml.MapInstanceId);

            mapObject.Effect =
                (short)(minilandobject?.ItemInstance?.Item?.EffectValue ?? minilandobject?.ItemInstance?.Design ?? 0);
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
