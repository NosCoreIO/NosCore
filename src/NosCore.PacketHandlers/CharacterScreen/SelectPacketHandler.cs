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
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class SelectPacketHandler : PacketHandler<SelectPacket>, IWorldPacketHandler
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<InventoryItemInstanceDto, Guid> _inventoryItemInstanceDao;
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly IItemGenerationService _itemProvider;
        private readonly ILogger _logger;
        private readonly IMapInstanceAccessorService _mapInstanceAccessorService;
        private readonly IDao<QuicklistEntryDto, Guid> _quickListEntriesDao;
        private readonly IDao<StaticBonusDto, long> _staticBonusDao;
        private readonly IDao<TitleDto, Guid> _titleDao;
        private readonly IDao<CharacterQuestDto, Guid> _characterQuestDao;
        private readonly IDao<ScriptDto, Guid> _scriptDao;
        private readonly List<QuestObjectiveDto> _questObjectives;
        private readonly List<QuestDto> _quests;
        private readonly IOptions<WorldConfiguration> _configuration;

        public SelectPacketHandler(IDao<CharacterDto, long> characterDao, ILogger logger,
            IItemGenerationService itemProvider,
            IMapInstanceAccessorService mapInstanceAccessorService, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quickListEntriesDao, IDao<TitleDto, Guid> titleDao, IDao<CharacterQuestDto, Guid> characterQuestDao,
            IDao<ScriptDto, Guid> scriptDao, List<QuestDto> quests, List<QuestObjectiveDto> questObjectives, IOptions<WorldConfiguration> configuration)
        {
            _characterDao = characterDao;
            _logger = logger;
            _mapInstanceAccessorService = mapInstanceAccessorService;
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
            _configuration = configuration;
        }

        public override async Task ExecuteAsync(SelectPacket packet, ClientSession clientSession)
        {
            try
            {
                var characterDto = await
                    _characterDao.FirstOrDefaultAsync(s =>
                        (s.AccountId == clientSession.Account.AccountId) && (s.Slot == packet.Slot)
                        && (s.State == CharacterState.Active) && s.ServerId == _configuration.Value.ServerId).ConfigureAwait(false);
                if (characterDto == null)
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHARACTER_SLOT_EMPTY), new
                    {
                        clientSession.Account.AccountId,
                        packet.Slot
                    });
                    return;
                }

                var character = characterDto.Adapt<Character>();

                character.MapInstanceId = _mapInstanceAccessorService.GetBaseMapInstanceIdByMapId(character.MapId);
                character.MapInstance = _mapInstanceAccessorService.GetMapInstance(character.MapInstanceId)!;
                character.PositionX = character.MapX;
                character.PositionY = character.MapY;
                character.Direction = 2;
                character.Script = character.CurrentScriptId != null ? await _scriptDao.FirstOrDefaultAsync(s => s.Id == character.CurrentScriptId).ConfigureAwait(false) : null;
                character.Group!.JoinGroup(character);

                var inventories = _inventoryItemInstanceDao
                    .Where(s => s.CharacterId == character.CharacterId)
                    ?.ToList() ?? new List<InventoryItemInstanceDto>();
                var ids = inventories.Select(o => o.ItemInstanceId).ToArray();
                var items = _itemInstanceDao.Where(s => ids.Contains(s!.Id))?.ToList() ?? new List<IItemInstanceDto?>();
                inventories.ForEach(k => character.InventoryService[k.ItemInstanceId] =
                    InventoryItemInstance.Create(_itemProvider.Convert(items.First(s => s!.Id == k.ItemInstanceId)!),
                        character.CharacterId, k));
                await clientSession.SetCharacterAsync(character).ConfigureAwait(false);

#pragma warning disable CS0618
                await clientSession.SendPacketsAsync(clientSession.Character.GenerateInv()).ConfigureAwait(false);
#pragma warning restore CS0618
                await clientSession.SendPacketAsync(clientSession.Character.GenerateMlobjlst()).ConfigureAwait(false);
                if (clientSession.Character.Hp > clientSession.Character.MaxHp)
                {
                    clientSession.Character.Hp = clientSession.Character.MaxHp;
                }

                if (clientSession.Character.Mp > clientSession.Character.MaxMp)
                {
                    clientSession.Character.Mp = clientSession.Character.MaxMp;
                }

                var quests = _characterQuestDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId) ?? new List<CharacterQuestDto>();
                clientSession.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>(quests.ToDictionary(x => x.Id, x =>
                    {
                        var charquest = x.Adapt<CharacterQuest>();
                        charquest.Quest = _quests.First(s => s.QuestId == charquest.QuestId).Adapt<GameObject.Services.QuestService.Quest>();
                        charquest.Quest.QuestObjectives =
                            _questObjectives.Where(s => s.QuestId == charquest.QuestId).ToList();
                        return charquest;
                    }));
                clientSession.Character.QuicklistEntries = _quickListEntriesDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId)?.ToList() ?? new List<QuicklistEntryDto>();
                clientSession.Character.StaticBonusList = _staticBonusDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId)?.ToList() ?? new List<StaticBonusDto>();
                clientSession.Character.Titles = _titleDao
                    .Where(s => s.CharacterId == clientSession.Character.CharacterId)?.ToList() ?? new List<TitleDto>();
                await clientSession.SendPacketAsync(new OkPacket()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHARACTER_SELECTION_FAILED), ex, new
                {
                    clientSession.Account.AccountId,
                    packet.Slot
                });
            }
        }
    }
}