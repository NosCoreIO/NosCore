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
    public class SaveService(IDao<CharacterDto, long> characterDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<AccountDto, long> accountDao,
            IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quicklistEntriesDao, IDao<MinilandDto, Guid> minilandDao,
            IMinilandService minilandProvider, IDao<TitleDto, Guid> titleDao,
            IDao<CharacterQuestDto, Guid> characterQuestDao, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : ISaveService
    {
        public async Task SaveAsync(ICharacterEntity chara)
        {
            if (chara is not Character character)
            {
                return;
            }

            try
            {
                var account = character.Account;
                await accountDao.TryInsertOrUpdateAsync(account).ConfigureAwait(false);

                await characterDao.TryInsertOrUpdateAsync(character).ConfigureAwait(false);

                var quicklistEntriesToDelete = quicklistEntriesDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.QuicklistEntries.All(o => o.Id != i.Id)).ToList();
                await quicklistEntriesDao.TryDeleteAsync(quicklistEntriesToDelete.Select(s => s.Id).ToArray()).ConfigureAwait(false);
                await quicklistEntriesDao.TryInsertOrUpdateAsync(character.QuicklistEntries).ConfigureAwait(false);

                // load and concat inventory with equipment
                var itemsToDelete = inventoryItemInstanceDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.InventoryService.Values.All(o => o.Id != i.Id)).ToList();

                await inventoryItemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.Id).ToArray()).ConfigureAwait(false);
                await itemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.ItemInstanceId).ToArray()).ConfigureAwait(false);

                await itemInstanceDao.TryInsertOrUpdateAsync(character.InventoryService.Values.Select(s => s.ItemInstance!).ToArray()).ConfigureAwait(false);
                await inventoryItemInstanceDao.TryInsertOrUpdateAsync(character.InventoryService.Values.ToArray()).ConfigureAwait(false);

                var staticBonusToDelete = staticBonusDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.StaticBonusList.All(o => o.StaticBonusId != i.StaticBonusId)).ToList();
                await staticBonusDao.TryDeleteAsync(staticBonusToDelete.Select(s => s.StaticBonusId)).ConfigureAwait(false);
                await staticBonusDao.TryInsertOrUpdateAsync(character.StaticBonusList).ConfigureAwait(false);

                await titleDao.TryInsertOrUpdateAsync(character.Titles).ConfigureAwait(false);

                var minilandDto = (MinilandDto)minilandProvider.GetMiniland(character.VisualId);
                await minilandDao.TryInsertOrUpdateAsync(minilandDto).ConfigureAwait(false);

                var questsToDelete = characterQuestDao
                        .Where(i => i.CharacterId == character.VisualId)!.ToList()
                    .Where(i => character.Quests.Values.All(o => o.QuestId != i.QuestId)).ToList();
                await characterQuestDao.TryDeleteAsync(questsToDelete.Select(s => s.Id)).ConfigureAwait(false);
                await characterQuestDao.TryInsertOrUpdateAsync(character.Quests.Values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Error(logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], character.CharacterId, e);
            }
        }
    }
}
