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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.SaveService
{
    public class SaveService : ISaveService
    {
        private readonly IDao<AccountDto, long> _accountDao;
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<InventoryItemInstanceDto, Guid> _inventoryItemInstanceDao;
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly IDao<MinilandDto, Guid> _minilandDao;
        private readonly IMinilandService _minilandProvider;
        private readonly IDao<QuicklistEntryDto, Guid> _quicklistEntriesDao;
        private readonly IDao<StaticBonusDto, long> _staticBonusDao;
        private readonly IDao<TitleDto, Guid> _titleDao;
        private readonly IDao<CharacterQuestDto, Guid> _characterQuestsDao;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public SaveService(IDao<CharacterDto, long> characterDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<AccountDto, long> accountDao,
            IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quicklistEntriesDao, IDao<MinilandDto, Guid> minilandDao,
            IMinilandService minilandProvider, IDao<TitleDto, Guid> titleDao, IDao<CharacterQuestDto, Guid> characterQuestDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _characterDao = characterDao;
            _itemInstanceDao = itemInstanceDao;
            _accountDao = accountDao;
            _inventoryItemInstanceDao = inventoryItemInstanceDao;
            _staticBonusDao = staticBonusDao;
            _titleDao = titleDao;
            _quicklistEntriesDao = quicklistEntriesDao;
            _characterQuestsDao = characterQuestDao;
            _minilandDao = minilandDao;
            _minilandProvider = minilandProvider;
            _logger = logger;
            _logLanguage = logLanguage;
        }

        public async Task SaveAsync(ICharacterEntity chara)
        {
            if (chara is not Character character)
            {
                return;
            }

            try

            {
                var account = character.Session.Account;
                await _accountDao.TryInsertOrUpdateAsync(account).ConfigureAwait(false);

                await _characterDao.TryInsertOrUpdateAsync(character).ConfigureAwait(false);

                var quicklistEntriesToDelete = _quicklistEntriesDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.QuicklistEntries.All(o => o.Id != i.Id)).ToList();
                await _quicklistEntriesDao.TryDeleteAsync(quicklistEntriesToDelete.Select(s => s.Id).ToArray()).ConfigureAwait(false);
                await _quicklistEntriesDao.TryInsertOrUpdateAsync(character.QuicklistEntries).ConfigureAwait(false);

                // load and concat inventory with equipment
                var itemsToDelete = _inventoryItemInstanceDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.InventoryService.Values.All(o => o.Id != i.Id)).ToList();

                await _inventoryItemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.Id).ToArray()).ConfigureAwait(false);
                await _itemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.ItemInstanceId).ToArray()).ConfigureAwait(false);

                await _itemInstanceDao.TryInsertOrUpdateAsync(character.InventoryService.Values.Select(s => s.ItemInstance!).ToArray()).ConfigureAwait(false);
                await _inventoryItemInstanceDao.TryInsertOrUpdateAsync(character.InventoryService.Values.ToArray()).ConfigureAwait(false);

                var staticBonusToDelete = _staticBonusDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.StaticBonusList.All(o => o.StaticBonusId != i.StaticBonusId)).ToList();
                await _staticBonusDao.TryDeleteAsync(staticBonusToDelete.Select(s => s.StaticBonusId)).ConfigureAwait(false);
                await _staticBonusDao.TryInsertOrUpdateAsync(character.StaticBonusList).ConfigureAwait(false);

                await _titleDao.TryInsertOrUpdateAsync(character.Titles).ConfigureAwait(false);

                var minilandDto = (MinilandDto)_minilandProvider.GetMiniland(character.VisualId);
                await _minilandDao.TryInsertOrUpdateAsync(minilandDto).ConfigureAwait(false);

                var questsToDelete = _characterQuestsDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.Quests.Values.All(o => o.QuestId != i.QuestId)).ToList();
                await _characterQuestsDao.TryDeleteAsync(questsToDelete.Select(s => s.Id)).ConfigureAwait(false);
                await _characterQuestsDao.TryInsertOrUpdateAsync(character.Quests.Values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(_logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], character.Session.SessionId, e);
            }
        }
    }
}
