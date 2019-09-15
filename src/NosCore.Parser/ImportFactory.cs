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

using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Data;
using NosCore.Data.Enumerations.Account;
using NosCore.Parser.Parsers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NosCore.Parser
{
    public class ImportFactory
    {
        private readonly List<string[]> _packetList = new List<string[]>();
        private string _folder;

        private readonly CardParser _cardParser;
        private readonly DropParser _dropParser;
        private readonly I18NParser _i18NParser;
        private readonly ItemParser _itemParser;
        private readonly MapMonsterParser _mapMonsterParser;
        private readonly MapNpcParser _mapNpcParser;
        private readonly MapParser _mapParser;
        private readonly MapTypeMapParser _mapTypeMapParser;
        private readonly MapTypeParser _mapTypeParser;
        private readonly NpcMonsterParser _npcMonsterParser;
        private readonly PortalParser _portalParser;
        private readonly RespawnMapTypeParser _respawnMapTypeParser;
        private readonly ShopItemParser _shopItemParser;
        private readonly ShopParser _shopParser;
        private readonly SkillParser _skillParser;
        private readonly IGenericDao<AccountDto> _accountDao;
        public ImportFactory(CardParser cardParser, DropParser dropParser, I18NParser i18NParser, ItemParser itemParser, MapMonsterParser mapMonsterParser,
            MapNpcParser mapNpcParser, MapParser mapParser, MapTypeMapParser mapTypeMapParser, MapTypeParser mapTypeParser, NpcMonsterParser npcMonsterParser,
            PortalParser portalParser, RespawnMapTypeParser respawnMapTypeParser,
            ShopItemParser shopItemParser, ShopParser shopParser, SkillParser skillParser, IGenericDao<AccountDto> accountDao)
        {
            _cardParser = cardParser;
            _dropParser = dropParser;
            _i18NParser = i18NParser;
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
            _skillParser = skillParser;
            _accountDao = accountDao;
        }

        public void ImportAccounts()
        {
            var acc1 = new AccountDto
            {
                Authority = AuthorityType.GameMaster,
                Name = "admin",
                Password = "test".ToSha512()
            };

            if (_accountDao.FirstOrDefault(s => s.Name == acc1.Name) == null)
            {
                _accountDao.InsertOrUpdate(ref acc1);
            }

            var acc2 = new AccountDto
            {
                Authority = AuthorityType.User,
                Name = "test",
                Password = "test".ToSha512()
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
            var filePacket = $"{_folder}\\packet.txt";
            using (var packetTxtStream =
                new StreamReader(filePacket, Encoding.Default))
            {
                string line;
                while ((line = packetTxtStream.ReadLine()) != null)
                {
                    var linesave = line.Split(' ');
                    _packetList.Add(linesave);
                }
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
            _i18NParser.InsertI18N(_folder);
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