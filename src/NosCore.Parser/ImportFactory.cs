using System.Collections.Generic;
using System.IO;
using System.Text;
using NosCore.Configuration;
using NosCore.Core.Encryption;
using NosCore.Data;
using NosCore.DAL;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Parser
{
	public class ImportFactory
	{
		#region Members

		private readonly string _folder;
		private readonly List<string[]> _packetList = new List<string[]>();

		private readonly MapParser _mapParser = new MapParser();
		private readonly MapNpcParser _mapNpcParser = new MapNpcParser();
		private readonly CardParser _cardParser = new CardParser();
		private readonly ItemParser _itemParser = new ItemParser();
		private readonly PortalParser _portalParser = new PortalParser();
		private readonly I18NParser _i18NParser = new I18NParser();
        private readonly NpcMonsterParser _npcMonsterParser = new NpcMonsterParser();
        private readonly NpcMonsterDataParser _npcMonsterDataParser = new NpcMonsterDataParser();
        private readonly MapMonsterParser _mapMonsterParser = new MapMonsterParser();

        private readonly ParserConfiguration configuration;

		public ImportFactory(string folder, ParserConfiguration conf)
		{
			configuration = conf;
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
			_cardParser.InsertCards();
		}

		public void ImportMapNpcs()
		{
			_mapNpcParser.InsertMapNpcs(_packetList);
		}

		public void ImportMaps()
		{
			_mapParser.InsertOrUpdateMaps(_folder, _packetList);
		}

		public void ImportQuests()
		{
		}

		public void ImportMapType()
		{
		}

		public void ImportMapTypeMap()
		{
		}

		public void ImportMonsters()
		{
            _mapMonsterParser.ImportMonsters(_packetList);
		}

		public void ImportNpcMonsterData()
		{
            _npcMonsterDataParser.InserNpcMonsterData(_packetList);

        }

		public void ImportNpcMonsters()
		{
            _npcMonsterParser.ImportNpcMonsters(_folder);
		}

		public void ImportPackets()
		{
			var filePacket = $"{_folder}\\packet.txt";
			using (var packetTxtStream =
				new StreamReader(filePacket, CodePagesEncodingProvider.Instance.GetEncoding(1252)))
			{
				string line;
				while ((line = packetTxtStream.ReadLine()) != null)
				{
					var linesave = line.Split(' ');
					_packetList.Add(linesave);
				}
			}
		}

		public void ImportPortals()
		{
			_portalParser.InsertPortals(_packetList);
		}

		public void ImportI18N()
		{
			_i18NParser.InsertI18N(_folder);
		}

		public void ImportRecipe()
		{
		}

		public void ImportRespawnMapType()
		{
		}

		public void ImportShopItems()
		{
		}

		public void ImportShops()
		{
		}

		public void ImportShopSkills()
		{
		}

		public void ImportSkills()
		{
		}

		public void ImportTeleporters()
		{
		}

		public void ImportScriptedInstances()
		{
		}

		internal void ImportItems()
		{
			_itemParser.Parse(_folder, _packetList);
		}

		#endregion
	}
}