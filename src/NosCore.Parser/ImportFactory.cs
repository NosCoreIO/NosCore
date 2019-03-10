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
using NosCore.Core.Encryption;
using NosCore.Data;
using NosCore.Database.DAL;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Parser
{
    public class ImportFactory
    {
        private readonly CardParser _cardParser = new CardParser();
        private readonly DropParser _dropParser = new DropParser();

        private readonly string _folder;
        private readonly I18NParser _i18NParser = new I18NParser();
        private readonly ItemParser _itemParser = new ItemParser();
        private readonly MapMonsterParser _mapMonsterParser = new MapMonsterParser();
        private readonly MapNpcParser _mapNpcParser = new MapNpcParser();

        private readonly MapParser _mapParser = new MapParser();
        private readonly MapTypeMapParser _mapTypeMapParser = new MapTypeMapParser();
        private readonly MapTypeParser _mapTypeParser = new MapTypeParser();
        private readonly NpcMonsterParser _npcMonsterParser = new NpcMonsterParser();
        private readonly List<string[]> _packetList = new List<string[]>();
        private readonly PortalParser _portalParser = new PortalParser();
        private readonly RespawnMapTypeParser _respawnMapTypeParser = new RespawnMapTypeParser();
        private readonly ShopItemParser _shopItemParser = new ShopItemParser();
        private readonly ShopParser _shopParser = new ShopParser();
        private readonly SkillParser _skillParser = new SkillParser();
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>();
        public ImportFactory(string folder)
        {
            _folder = folder;
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
    }
}