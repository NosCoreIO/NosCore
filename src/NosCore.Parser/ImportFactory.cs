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

using System.Collections.Generic;
using System.IO;
using System.Text;
using NosCore.Core;
using NosCore.Core.Encryption;
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
        private readonly IGenericDao<AccountDto> _accountDao;

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
        private readonly IGenericDao<I18NActDescDto> _i18NActDescDao;
        private readonly IGenericDao<I18NBCardDto> _i18NbCardDao;
        private readonly IGenericDao<I18NCardDto> _i18NCardDao;
        private readonly IGenericDao<I18NItemDto> _i18NItemDao;
        private readonly IGenericDao<I18NMapIdDataDto> _i18NMapIdDataDao;
        private readonly IGenericDao<I18NMapPointDataDto> _i18NMapPointDataDao;
        private readonly IGenericDao<I18NNpcMonsterDto> _i18NNpcMonsterDao;
        private readonly IGenericDao<I18NNpcMonsterTalkDto> _i18NNpcMonsterTalkDao;
        private readonly IGenericDao<I18NQuestDto> _i18NQuestDao;
        private readonly IGenericDao<I18NSkillDto> _i18NSkillDao;
        private readonly ILogger _logger;
        private readonly string password = "test".ToSha512();
        private string _folder = "";

        public ImportFactory(CardParser cardParser, DropParser dropParser, ItemParser itemParser,
            MapMonsterParser mapMonsterParser,
            MapNpcParser mapNpcParser, MapParser mapParser, MapTypeMapParser mapTypeMapParser,
            MapTypeParser mapTypeParser, NpcMonsterParser npcMonsterParser,
            PortalParser portalParser, RespawnMapTypeParser respawnMapTypeParser,
            ShopItemParser shopItemParser, ShopParser shopParser, SkillParser skillParser,
            QuestPrizeParser questPrizeParser, QuestParser questParser, ActParser actParser, ScriptParser scriptParser,
            IGenericDao<AccountDto> accountDao, IGenericDao<I18NQuestDto> i18NQuestDao, IGenericDao<I18NSkillDto> i18NSkillDao,
            IGenericDao<I18NNpcMonsterTalkDto> i18NNpcMonsterTalkDao,
            IGenericDao<I18NNpcMonsterDto> i18NNpcMonsterDao, IGenericDao<I18NMapPointDataDto> i18NMapPointDataDao,
            IGenericDao<I18NMapIdDataDto> i18NMapIdDataDao,
            IGenericDao<I18NItemDto> i18NItemDao, IGenericDao<I18NBCardDto> i18NbCardDao,
            IGenericDao<I18NCardDto> i18NCardDao, IGenericDao<I18NActDescDto> i18NActDescDao, ILogger logger)
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

        public void ImportAccounts()
        {
            var acc1 = new AccountDto
            {
                Authority = AuthorityType.GameMaster,
                Name = "admin",
                Password = password
            };

            if (_accountDao.FirstOrDefault(s => s.Name == acc1.Name) == null)
            {
                _accountDao.InsertOrUpdate(ref acc1);
            }

            var acc2 = new AccountDto
            {
                Authority = AuthorityType.User,
                Name = "test",
                Password = password
            };

            if (_accountDao.FirstOrDefault(s => s.Name == acc1.Name) == null)
            {
                _accountDao.InsertOrUpdate(ref acc2);
            }
        }

        public void ImportCards()
        {
            _cardParser.InsertCards(_folder);
        }

        public void ImportMapNpcs()
        {
            _mapNpcParser.InsertMapNpcs(_packetList);
        }

        public void ImportMapMonsters()
        {
            _mapMonsterParser.InsertMapMonster(_packetList);
        }

        public void ImportShops()
        {
            _shopParser.InsertShops(_packetList);
        }

        public void ImportShopItems()
        {
            _shopItemParser.InsertShopItems(_packetList);
        }

        public void ImportMaps()
        {
            _mapParser.InsertOrUpdateMaps(_folder, _packetList);
        }

        public void ImportScripts()
        {
            _scriptParser.InsertScripts(_folder);
        }

        public void ImportQuests()
        {
            _actParser.ImportAct(_folder);
            _questPrizeParser.ImportQuestPrizes(_folder);
            _questParser.ImportQuests(_folder);
        }

        public void ImportMapType()
        {
            _mapTypeParser.InsertMapTypes();
        }

        public void ImportMapTypeMap()
        {
            _mapTypeMapParser.InsertMapTypeMaps();
        }

        public void ImportNpcMonsters()
        {
            _npcMonsterParser.InsertNpcMonsters(_folder);
        }

        internal void ImportRespawnMapType()
        {
            _respawnMapTypeParser.InsertRespawnMapType();
        }

        public void ImportPackets()
        {
            var filePacket = $"{_folder}{Path.DirectorySeparatorChar}packet.txt";
            using var packetTxtStream =
                new StreamReader(filePacket, Encoding.Default);
            string? line;
            while ((line = packetTxtStream.ReadLine()) != null)
            {
                var linesave = line.Split(' ');
                _packetList.Add(linesave);
            }
        }

        internal void ImportSkills()
        {
            _skillParser.InsertSkills(_folder);
        }

        public void ImportDrops()
        {
            _dropParser.InsertDrop();
        }

        public void ImportPortals()
        {
            _portalParser.InsertPortals(_packetList);
        }

        public void ImportI18N()
        {
            new I18NParser<I18NActDescDto>(_i18NActDescDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_act_desc.txt", LogLanguageKey.I18N_ACTDESC_PARSED);
            new I18NParser<I18NBCardDto>(_i18NbCardDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_BCard.txt", LogLanguageKey.I18N_BCARD_PARSED);
            new I18NParser<I18NCardDto>(_i18NCardDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_Card.txt", LogLanguageKey.I18N_CARD_PARSED);
            new I18NParser<I18NItemDto>(_i18NItemDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_Item.txt", LogLanguageKey.I18N_ITEM_PARSED);
            new I18NParser<I18NMapIdDataDto>(_i18NMapIdDataDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapIDData.txt", LogLanguageKey.I18N_MAPIDDATA_PARSED);
            new I18NParser<I18NMapPointDataDto>(_i18NMapPointDataDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapPointData.txt", LogLanguageKey.I18N_MAPPOINTDATA_PARSED);
            new I18NParser<I18NNpcMonsterDto>(_i18NNpcMonsterDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_monster.txt", LogLanguageKey.I18N_MPCMONSTER_PARSED);
            new I18NParser<I18NQuestDto>(_i18NQuestDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_quest.txt", LogLanguageKey.I18N_QUEST_PARSED);
            new I18NParser<I18NSkillDto>(_i18NSkillDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_Skill.txt", LogLanguageKey.I18N_SKILL_PARSED);
            new I18NParser<I18NNpcMonsterTalkDto>(_i18NNpcMonsterTalkDao, _logger).InsertI18N(_folder + Path.DirectorySeparatorChar + "_code_{0}_npctalk.txt", LogLanguageKey.I18N_NPCMONSTERTALK_PARSED);
        }

        internal void ImportItems()
        {
            _itemParser.Parse(_folder);
        }

        public void SetFolder(string folder)
        {
            _folder = folder;
        }
    }
}