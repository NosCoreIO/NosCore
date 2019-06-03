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
using System.Linq;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ServerPackets.CharacterSelectionScreen;
using Mapster;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.MapInstanceProvider;
using Serilog;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class SelectPacketHandler : PacketHandler<SelectPacket>, IWorldPacketHandler
    {
        private readonly IAdapter _adapter;
        private readonly ILogger _logger;
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IMapInstanceProvider _mapInstanceProvider;
        private readonly IItemProvider _itemProvider;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IGenericDao<InventoryItemInstanceDto> _inventoryItemInstanceDao;

        public SelectPacketHandler(IAdapter adapter, IGenericDao<CharacterDto> characterDao, ILogger logger, IItemProvider itemProvider, 
            IMapInstanceProvider mapInstanceProvider, IGenericDao<IItemInstanceDto> itemInstanceDao, IGenericDao<InventoryItemInstanceDto> inventoryItemInstanceDao)
        {
            _adapter = adapter;
            _characterDao = characterDao;
            _logger = logger;
            _mapInstanceProvider = mapInstanceProvider;
            _itemProvider = itemProvider;
            _itemInstanceDao = itemInstanceDao;
            _inventoryItemInstanceDao = inventoryItemInstanceDao;
        }
     
        public override void Execute(SelectPacket packet, ClientSession clientSession)
        {
            try
            {
                if (clientSession?.Account == null || clientSession.HasSelectedCharacter)
                {
                    return;
                }

                var characterDto =
                    _characterDao.FirstOrDefault(s =>
                        s.AccountId == clientSession.Account.AccountId && s.Slot == packet.Slot
                        && s.State == CharacterState.Active);
                if (characterDto == null)
                {
                    return;
                }

                var character = _adapter.Adapt<Character>(characterDto);

                character.MapInstanceId = _mapInstanceProvider.GetBaseMapInstanceIdByMapId(character.MapId);
                character.MapInstance = _mapInstanceProvider.GetMapInstance(character.MapInstanceId);
                character.PositionX = character.MapX;
                character.PositionY = character.MapY;
                character.Direction = 2;
                character.Account = clientSession.Account;
                character.Group.JoinGroup(character);
                clientSession.SetCharacter(character);

                var inventories = _inventoryItemInstanceDao
                    .Where(s => s.CharacterId == character.CharacterId)
                    .ToList();
                var ids = inventories.Select(o => o.ItemInstanceId).ToArray();
                var items = _itemInstanceDao.Where(s => ids.Contains(s.Id)).ToList();
                inventories.ForEach(k => character.Inventory[k.Id] = InventoryItemInstance.Create(_itemProvider.Convert(items.First(s=>s.Id == k.ItemInstanceId)), character.CharacterId, k));

#pragma warning disable CS0618
                clientSession.SendPackets(clientSession.Character.GenerateInv());
#pragma warning restore CS0618

                if (clientSession.Character.Hp > clientSession.Character.HpLoad())
                {
                    clientSession.Character.Hp = (int)clientSession.Character.HpLoad();
                }

                if (clientSession.Character.Mp > clientSession.Character.MpLoad())
                {
                    clientSession.Character.Mp = (int)clientSession.Character.MpLoad();
                }

                //var relations =
                //    _characterRelationDao.Where(s => s.CharacterId == Session.Character.CharacterId);
                //var relationsWithCharacter =
                //    _characterRelationDao.Where(s => s.RelatedCharacterId == Session.Character.CharacterId);

                //var characters = _characterDao
                //    .Where(s => relations.Select(v => v.RelatedCharacterId).Contains(s.CharacterId)).ToList();
                //var relatedCharacters = _characterDao.Where(s =>
                //    relationsWithCharacter.Select(v => v.RelatedCharacterId).Contains(s.CharacterId)).ToList();

                //foreach (var relation in _adapter.Adapt<IEnumerable<CharacterRelation>>(relations))
                //{
                //    relation.CharacterName = characters.Find(s => s.CharacterId == relation.RelatedCharacterId)?.Name;
                //    Session.Character.CharacterRelations[relation.CharacterRelationId] = relation;
                //}

                //foreach (var relation in _adapter.Adapt<IEnumerable<CharacterRelation>>(relationsWithCharacter))
                //{
                //    relation.CharacterName =
                //        relatedCharacters.Find(s => s.CharacterId == relation.RelatedCharacterId)?.Name;
                //    Session.Character.RelationWithCharacter[relation.CharacterRelationId] = relation;
                //}

                clientSession.SendPacket(new OkPacket());
            }
            catch (Exception ex)
            {
                _logger.Error("Select character failed.", ex);
            }

        }
    }

}