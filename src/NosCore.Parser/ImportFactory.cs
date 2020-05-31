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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core.Encryption;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.I18N;
using NosCore.Parser.Parsers;
using Serilog;

namespace NosCore.Parser
{
    public class ImportFactory
    {
        private readonly IDao<AccountDto, long> _accountDao;

        private readonly CardParser _cardParser;
        private readonly DropParser _dropParser;
        private readonly ItemParser _itemParser;
        private readonly MapMonsterParser _mapMonsterParser;
        private readonly MapNpcParser _mapNpcParser;
        private readonly MapParser _mapParser;
        private readonly MapTypeMapParser _mapTypeMapParser;
        private readonly QuestParser _questParser;
        private readonly QuestPrizeParser _questPrizeParser;
        private readonly MapTypeParser _mapTypeParser;
        private readonly NpcMonsterParser _npcMonsterParser;
        private readonly List<string[]> _packetList = new List<string[]>();
        private readonly PortalParser _portalParser;
        private readonly RespawnMapTypeParser _respawnMapTypeParser;
        private readonly ShopItemParser _shopItemParser;
        private readonly ShopParser _shopParser;
        private readonly SkillParser _skillParser;
        private readonly ActParser _actParser;
        private readonly ScriptParser _scriptParser;
        private readonly NpcTalkParser _npcTalkParser;
        private readonly IDao<I18NActDescDto, int> _i18NActDescDao;
        private readonly IDao<I18NBCardDto, int> _i18NbCardDao;
        private readonly IDao<I18NCardDto, int> _i18NCardDao;
        private readonly IDao<I18NItemDto, int> _i18NItemDao;
        private readonly IDao<I18NMapIdDataDto, int> _i18NMapIdDataDao;
        private readonly IDao<I18NMapPointDataDto, int> _i18NMapPointDataDao;
        private readonly IDao<I18NNpcMonsterDto, int> _i18NNpcMonsterDao;
        private readonly IDao<I18NNpcMonsterTalkDto, int> _i18NNpcMonsterTalkDao;
        private readonly IDao<I18NQuestDto, int> _i18NQuestDao;
        private readonly IDao<I18NSkillDto, int> _i18NSkillDao;
        private readonly ILogger _logger;
        private readonly string _password = "test".ToSha512();
        private string _folder = "";

        public ImportFactory(CardParser cardParser, DropParser dropParser, ItemParser itemParser,
            MapMonsterParser mapMonsterParser,
            MapNpcParser mapNpcParser, MapParser mapParser, MapTypeMapParser mapTypeMapParser,
            MapTypeParser mapTypeParser, NpcMonsterParser npcMonsterParser,
            PortalParser portalParser, RespawnMapTypeParser respawnMapTypeParser,
            ShopItemParser shopItemParser, ShopParser shopParser, SkillParser skillParser, NpcTalkParser npcTalkParser,
            QuestPrizeParser questPrizeParser, QuestParser questParser, ActParser actParser, ScriptParser scriptParser,
            IDao<AccountDto, long> accountDao, IDao<I18NQuestDto, int> i18NQuestDao, IDao<I18NSkillDto, int> i18NSkillDao,
            IDao<I18NNpcMonsterTalkDto, int> i18NNpcMonsterTalkDao,
            IDao<I18NNpcMonsterDto, int> i18NNpcMonsterDao, IDao<I18NMapPointDataDto, int> i18NMapPointDataDao,
            IDao<I18NMapIdDataDto, int> i18NMapIdDataDao,
            IDao<I18NItemDto, int> i18NItemDao, IDao<I18NBCardDto, int> i18NbCardDao,
            IDao<I18NCardDto, int> i18NCardDao, IDao<I18NActDescDto, int> i18NActDescDao, ILogger logger)
        {
            _actParser = actParser;
            _questPrizeParser = questPrizeParser;
            _questParser = questParser;
            _cardParser = cardParser;
            _dropParser = dropParser;
            _itemParser = itemParser;
            _mapMonsterParser = mapMonsterParser;
            _mapNpcParser = mapNpcParser;
            _mapParser = mapParser;
            _mapTypeMapParser = mapTypeMapParser;
            _mapTypeParser = mapTypeParser;
            _npcMonsterParser = npcMonsterParser;
            _portalParser = portalParser;
            _respawnMapTypeParser = respawnMapTypeParser;
            _shopItemParser = shopItemParser;
            _shopParser = shopParser;
            _scriptParser = scriptParser;
            _skillParser = skillParser;
            _npcTalkParser = npcTalkParser;
            _accountDao = accountDao;
            _i18NQuestDao = i18NQuestDao;
            _i18NSkillDao = i18NSkillDao;
            _i18NNpcMonsterTalkDao = i18NNpcMonsterTalkDao;
            _i18NNpcMonsterDao = i18NNpcMonsterDao;
            _i18NMapPointDataDao = i18NMapPointDataDao;
            _i18NMapIdDataDao = i18NMapIdDataDao;
            _i18NItemDao = i18NItemDao;
            _i18NbCardDao = i18NbCardDao;
            _i18NCardDao = i18NCardDao;
            _i18NActDescDao = i18NActDescDao;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task ImportAccountsAsync()
        {
            var acc1 = new AccountDto
            {
                Authority = AuthorityType.GameMaster,
                Name = "admin",
                Password = _password
            };

            if (await _accountDao.FirstOrDefaultAsync(s => s.Name == acc1.Name).ConfigureAwait(false) == null)
            {
                acc1 = await _accountDao.TryInsertOrUpdateAsync(acc1).ConfigureAwait(false);
            }

            var acc2 = new AccountDto
            {
                Authority = AuthorityType.User,
                Name = "test",
                Password = _password
            };

            if (await _accountDao.FirstOrDefaultAsync(s => s.Name == acc1.Name).ConfigureAwait(false) == null)
            {
                acc2 = await _accountDao.TryInsertOrUpdateAsync(acc2).ConfigureAwait(false);
            }
        }

        public Task ImportCardsAsync()
        {
            return _cardParser.InsertCardsAsync(_folder);
        }

        public async Task ImportMapNpcsAsync()
        {
            await _npcTalkParser.ParseAsync(_folder).ConfigureAwait(false);
            await _mapNpcParser.InsertMapNpcsAsync(_packetList).ConfigureAwait(false);
        }

        public Task ImportMapMonstersAsync()
        {
            return _mapMonsterParser.InsertMapMonsterAsync(_packetList);
        }

        public Task ImportShopsAsync()
        {
            return  _shopParser.InsertShopsAsync(_packetList);
        }

        public Task ImportShopItemsAsync()
        {
            return  _shopItemParser.InsertShopItemsAsync(_packetList);
        }

        public Task ImportMapsAsync()
        {
            return _mapParser.InsertOrUpdateMapsAsync(_folder, _packetList);
        }

        public Task ImportScriptsAsync()
        {
            return _scriptParser.InsertScriptsAsync(_folder);
        }

        public async Task ImportQuestsAsync()
        {
            await _actParser.ImportActAsync(_folder).ConfigureAwait(false);
            await _questPrizeParser.ImportQuestPrizesAsync(_folder).ConfigureAwait(false);
            await _questParser.ImportQuestsAsync(_folder).ConfigureAwait(false);
        }

        public Task ImportMapTypeAsync()
        {
            return _mapTypeParser.InsertMapTypesAsync();
        }

        public Task ImportMapTypeMapAsync()
        {
            return _mapTypeMapParser.InsertMapTypeMapsAsync();
        }

        public Task ImportNpcMonstersAsync()
        {
            return _npcMonsterParser.InsertNpcMonstersAsync(_folder);
        }

        public Task ImportRespawnMapTypeAsync()
        {
            return _respawnMapTypeParser.InsertRespawnMapTypeAsync();
        }

        public async Task ImportPacketsAsync()
        {
            var filePacket = $"{_folder}{Path.DirectorySeparatorChar}packet.txt";
            using var packetTxtStream =
                new StreamReader(filePacket, Encoding.Default);
            var lines = (await packetTxtStream.ReadToEndAsync().ConfigureAwait(false)).Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );
            foreach (var line in lines)
            {
                var linesave = line.Split(' ');
                _packetList.Add(linesave);
            }
        }

        public Task ImportSkillsAsync()
        {
            return _skillParser.InsertSkillsAsync(_folder);
        }

        public Task ImportDropsAsync()
        {
            return _dropParser.InsertDropAsync();
        }

        public Task ImportPortalsAsync()
        {
            return _portalParser.InsertPortalsAsync(_packetList);
        }

        public async Task ImportI18NAsync()
        {
           await new I18NParser<I18NActDescDto, int>(_i18NActDescDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_act_desc.txt", LogLanguageKey.I18N_ACTDESC_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NBCardDto, int>(_i18NbCardDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_BCard.txt", LogLanguageKey.I18N_BCARD_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NCardDto, int>(_i18NCardDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Card.txt", LogLanguageKey.I18N_CARD_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NItemDto, int>(_i18NItemDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Item.txt", LogLanguageKey.I18N_ITEM_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NMapIdDataDto, int>(_i18NMapIdDataDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapIDData.txt", LogLanguageKey.I18N_MAPIDDATA_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NMapPointDataDto, int>(_i18NMapPointDataDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapPointData.txt", LogLanguageKey.I18N_MAPPOINTDATA_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NNpcMonsterDto, int>(_i18NNpcMonsterDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_monster.txt", LogLanguageKey.I18N_MPCMONSTER_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NQuestDto, int>(_i18NQuestDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_quest.txt", LogLanguageKey.I18N_QUEST_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NSkillDto, int>(_i18NSkillDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Skill.txt", LogLanguageKey.I18N_SKILL_PARSED).ConfigureAwait(false);
           await new I18NParser<I18NNpcMonsterTalkDto, int>(_i18NNpcMonsterTalkDao, _logger).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_npctalk.txt", LogLanguageKey.I18N_NPCMONSTERTALK_PARSED).ConfigureAwait(false);
        }

        public Task ImportItemsAsync()
        {
            return _itemParser.ParseAsync(_folder);
        }

        public void SetFolder(string folder)
        {
            _folder = folder;
        }
    }
}