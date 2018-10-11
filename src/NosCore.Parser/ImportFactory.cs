using System.Collections.Generic;
using System.IO;
using System.Text;
using NosCore.Core.Encryption;
using NosCore.Data;
using NosCore.DAL;
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
        private readonly SkillParser _skillParser = new SkillParser();

        public ImportFactory(string folder)
        {
            _folder = folder;
        }

        public void ImportAccounts()
        {
            var acc1 = new AccountDTO
            {
                Authority = AuthorityType.GameMaster,
                Name = "admin",
                Password = EncryptionHelper.Sha512("test")
            };

            if (DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == acc1.Name) == null)
            {
                DAOFactory.AccountDAO.InsertOrUpdate(ref acc1);
            }

            var acc2 = new AccountDTO
            {
                Authority = AuthorityType.User,
                Name = "test",
                Password = EncryptionHelper.Sha512("test")
            };

            if (DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == acc1.Name) == null)
            {
                DAOFactory.AccountDAO.InsertOrUpdate(ref acc2);
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