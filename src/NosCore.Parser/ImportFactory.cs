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

using NosCore.Core.Encryption;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.I18N;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser
{
    public class ImportFactory(CardParser cardParser, DropParser dropParser, ItemParser itemParser,
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
        IDao<I18NCardDto, int> i18NCardDao, IDao<I18NActDescDto, int> i18NActDescDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly List<string[]> _packetList = new();
        private readonly string _password = new Sha512Hasher().Hash("test");
        private string _folder = "";

        public async Task ImportAccountsAsync()
        {
            var acc1 = new AccountDto
            {
                Authority = AuthorityType.GameMaster,
                Name = "admin",
                Password = _password
            };

            if (await accountDao.FirstOrDefaultAsync(s => s.Name == acc1.Name).ConfigureAwait(false) == null)
            {
                acc1 = await accountDao.TryInsertOrUpdateAsync(acc1).ConfigureAwait(false);
            }

            var acc2 = new AccountDto
            {
                Authority = AuthorityType.User,
                Name = "test",
                Password = _password
            };

            if (await accountDao.FirstOrDefaultAsync(s => s.Name == acc1.Name).ConfigureAwait(false) == null)
            {
                acc2 = await accountDao.TryInsertOrUpdateAsync(acc2).ConfigureAwait(false);
            }
        }

        public Task ImportCardsAsync()
        {
            return cardParser.InsertCardsAsync(_folder);
        }

        public async Task ImportMapNpcsAsync()
        {
            await npcTalkParser.ParseAsync(_folder).ConfigureAwait(false);
            await mapNpcParser.InsertMapNpcsAsync(_packetList).ConfigureAwait(false);
        }

        public Task ImportMapMonstersAsync()
        {
            return mapMonsterParser.InsertMapMonsterAsync(_packetList);
        }

        public Task ImportShopsAsync()
        {
            return shopParser.InsertShopsAsync(_packetList);
        }

        public Task ImportShopItemsAsync()
        {
            return shopItemParser.InsertShopItemsAsync(_packetList);
        }

        public Task ImportMapsAsync()
        {
            return mapParser.InsertOrUpdateMapsAsync(_folder, _packetList);
        }

        public Task ImportScriptsAsync()
        {
            return scriptParser.InsertScriptsAsync(_folder);
        }

        public async Task ImportQuestsAsync()
        {
            await actParser.ImportActAsync(_folder).ConfigureAwait(false);
            await questPrizeParser.ImportQuestPrizesAsync(_folder).ConfigureAwait(false);
            await questParser.ImportQuestsAsync(_folder).ConfigureAwait(false);
        }

        public Task ImportMapTypeAsync()
        {
            return mapTypeParser.InsertMapTypesAsync();
        }

        public Task ImportMapTypeMapAsync()
        {
            return mapTypeMapParser.InsertMapTypeMapsAsync();
        }

        public Task ImportNpcMonstersAsync()
        {
            return npcMonsterParser.InsertNpcMonstersAsync(_folder);
        }

        public Task ImportRespawnMapTypeAsync()
        {
            return respawnMapTypeParser.InsertRespawnMapTypeAsync();
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
            return skillParser.InsertSkillsAsync(_folder);
        }

        public Task ImportDropsAsync()
        {
            return dropParser.InsertDropAsync();
        }

        public Task ImportPortalsAsync()
        {
            return portalParser.InsertPortalsAsync(_packetList);
        }

        public async Task ImportI18NAsync()
        {
            await new I18NParser<I18NActDescDto, int>(i18NActDescDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_act_desc.txt", LogLanguageKey.I18N_ACTDESC_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NBCardDto, int>(i18NbCardDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_BCard.txt", LogLanguageKey.I18N_BCARD_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NCardDto, int>(i18NCardDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Card.txt", LogLanguageKey.I18N_CARD_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NItemDto, int>(i18NItemDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Item.txt", LogLanguageKey.I18N_ITEM_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NMapIdDataDto, int>(i18NMapIdDataDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapIDData.txt", LogLanguageKey.I18N_MAPIDDATA_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NMapPointDataDto, int>(i18NMapPointDataDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_MapPointData.txt", LogLanguageKey.I18N_MAPPOINTDATA_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NNpcMonsterDto, int>(i18NNpcMonsterDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_monster.txt", LogLanguageKey.I18N_MPCMONSTER_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NQuestDto, int>(i18NQuestDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_quest.txt", LogLanguageKey.I18N_QUEST_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NSkillDto, int>(i18NSkillDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_Skill.txt", LogLanguageKey.I18N_SKILL_PARSED).ConfigureAwait(false);
            await new I18NParser<I18NNpcMonsterTalkDto, int>(i18NNpcMonsterTalkDao, logger, logLanguage).InsertI18NAsync(_folder + Path.DirectorySeparatorChar + "_code_{0}_npctalk.txt", LogLanguageKey.I18N_NPCMONSTERTALK_PARSED).ConfigureAwait(false);
        }

        public Task ImportItemsAsync()
        {
            return itemParser.ParseAsync(_folder);
        }

        public void SetFolder(string folder)
        {
            _folder = folder;
        }
    }
}