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
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using Mapster;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
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
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IGenericDao<InventoryItemInstanceDto> _inventoryItemInstanceDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IItemProvider _itemProvider;
        private readonly ILogger _logger;
        private readonly IMapInstanceProvider _mapInstanceProvider;
        private readonly IGenericDao<QuicklistEntryDto> _quickListEntriesDao;
        private readonly IGenericDao<StaticBonusDto> _staticBonusDao;
        private readonly IGenericDao<TitleDto> _titleDao;
        private readonly IGenericDao<CharacterQuestDto> _characterQuestDao;
        private readonly IGenericDao<ScriptDto> _scriptDao;
        private readonly List<QuestObjectiveDto> _questObjectives;
        private readonly List<QuestDto> _quests;

        public SelectPacketHandler(IGenericDao<CharacterDto> characterDao, ILogger logger,
            IItemProvider itemProvider,
            IMapInstanceProvider mapInstanceProvider, IGenericDao<IItemInstanceDto> itemInstanceDao,
            IGenericDao<InventoryItemInstanceDto> inventoryItemInstanceDao, IGenericDao<StaticBonusDto> staticBonusDao,
            IGenericDao<QuicklistEntryDto> quickListEntriesDao, IGenericDao<TitleDto> titleDao, IGenericDao<CharacterQuestDto> characterQuestDao,
            IGenericDao<ScriptDto> scriptDao, List<QuestDto> quests, List<QuestObjectiveDto> questObjectives)
        {
            _characterDao = characterDao;
            _logger = logger;
            _mapInstanceProvider = mapInstanceProvider;
            _itemProvider = itemProvider;
            _itemInstanceDao = itemInstanceDao;
            _inventoryItemInstanceDao = inventoryItemInstanceDao;
            _staticBonusDao = staticBonusDao;
            _quickListEntriesDao = quickListEntriesDao;
            _titleDao = titleDao;
            _characterQuestDao = characterQuestDao;
            _scriptDao = scriptDao;
            _quests = quests;
            _questObjectives = questObjectives;
        }

        public override Task ExecuteAsync(SelectPacket packet, ClientSession clientSession)
        {
            try
            {
                if ((clientSession?.Account == null) || clientSession.HasSelectedCharacter)
                {
                    return Task.CompletedTask;
                }

                var characterDto =
                    _characterDao.FirstOrDefault(s =>
                        (s.AccountId == clientSession.Account.AccountId) && (s.Slot == packet.Slot)
                        && (s.State == CharacterState.Active));
                if (characterDto == null)
                {
                    return Task.CompletedTask;
                }

                var character = characterDto.Adapt<Character>();

                character.MapInstanceId = _mapInstanceProvider.GetBaseMapInstanceIdByMapId(character.MapId);
                character.MapInstance = _mapInstanceProvider.GetMapInstance(character.MapInstanceId)!;
                character.PositionX = character.MapX;
                character.PositionY = character.MapY;
                character.Direction = 2;
                character.Script = character.CurrentScriptId != null ? _scriptDao.FirstOrDefault(s => s.Id == character.CurrentScriptId) : null;
                character.Group!.JoinGroup(character);

                var inventories = _inventoryItemInstanceDao
                    .Where(s => s.CharacterId == character.CharacterId)
                    .ToList();
                var ids = inventories.Select(o => o.ItemInstanceId).ToArray();
                var items = _itemInstanceDao.Where(s => ids.Contains(s.Id)).ToList();
                inventories.ForEach(k => character.InventoryService[k.ItemInstanceId] =
                    InventoryItemInstance.Create(_itemProvider.Convert(items.First(s => s.Id == k.ItemInstanceId)),
                        character.CharacterId, k));
                clientSession.SetCharacter(character);

#pragma warning disable CS0618
                clientSession.SendPacketsAsync(clientSession.Character.GenerateInv());
#pragma warning restore CS0618
                clientSession.SendPacketAsync(clientSession.Character.GenerateMlobjlst());
                if (clientSession.Character.Hp > clientSession.Character.HpLoad())
                {
                    clientSession.Character.Hp = (int)clientSession.Character.HpLoad();
                }

                if (clientSession.Character.Mp > clientSession.Character.MpLoad())
                {
                    clientSession.Character.Mp = (int)clientSession.Character.MpLoad();
                }

                clientSession.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>(_characterQuestDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId).ToDictionary(x => x.Id, x =>
                    {
                        var charquest = x.Adapt<CharacterQuest>();
                        charquest.Quest = _quests.First(s => s.QuestId == charquest.QuestId).Adapt<GameObject.Quest>();
                        charquest.Quest.QuestObjectives =
                            _questObjectives.Where(s => s.QuestId == charquest.QuestId).ToList();
                        return charquest;
                    }));
                clientSession.Character.QuicklistEntries = _quickListEntriesDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId).ToList();
                clientSession.Character.StaticBonusList = _staticBonusDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId).ToList();
                clientSession.Character.Titles = _titleDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId).ToList();
                clientSession.SendPacketAsync(new OkPacket());
            }
            catch (Exception ex)
            {
                _logger.Error("Select character failed.", ex);
            }
            return Task.CompletedTask;
        }
    }
}