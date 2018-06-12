using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Data.I18N;
using NosCore.DAL;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
	public class I18NParser
	{
		private static readonly string ActDescTxt = "\\_code_{0}_act_desc.txt";
		private static readonly string BCardTxt = "\\_code_{0}_BCard.txt";
		private static readonly string CardTxt = "\\_code_{0}_Card.txt";
		private static readonly string ItemTxt = "\\_code_{0}_Item.txt";
		private static readonly string MapIDDataTxt = "\\_code_{0}_MapIDData.txt";
		private static readonly string MapPointDataTxt = "\\_code_{0}_MapPointData.txt";
		private static readonly string MonsterTxt = "\\_code_{0}_monster.txt";
		private static readonly string NpcTalkTxt = "\\_code_{0}_npctalk.txt";
		private static readonly string QuestTxt = "\\_code_{0}_quest.txt";
		private static readonly string SkillTxt = "\\_code_{0}_Skill.txt";

		private static string _line;
		private string _folder;

		private string I18NTextFileName(string textfilename, RegionType region)
		{
			var regioncode = region.ToString().ToLower();
			regioncode = regioncode == "en" ? "uk" : regioncode;
			return string.Format(_folder + textfilename, regioncode);
		}

		public void InsertI18N(string folder)
		{
			_folder = folder;
			var actdesclist = DAOFactory.I18N_ActDescDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var actdescdtos = new List<I18N_ActDescDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(ActDescTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (actdesclist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) ==
								null)
							{
								actdescdtos.Add(new I18N_ActDescDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_ActDescDAO.InsertOrUpdate(actdescdtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_ACTDESC_PARSED), actdescdtos.Count,
							region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var cardlist = DAOFactory.I18N_CardDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var carddtos = new List<I18N_CardDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(CardTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (cardlist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) == null)
							{
								carddtos.Add(new I18N_CardDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_CardDAO.InsertOrUpdate(carddtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_CARD_PARSED), carddtos.Count,
							region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var bcardlist = DAOFactory.I18N_BCardDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var bcarddtos = new List<I18N_BCardDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(BCardTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (bcardlist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) ==
								null)
							{
								bcarddtos.Add(new I18N_BCardDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_BCardDAO.InsertOrUpdate(bcarddtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_BCARD_PARSED), bcarddtos.Count,
							region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var itemlist = DAOFactory.I18N_ItemDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var itemdtos = new List<I18N_ItemDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(ItemTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (itemlist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) == null)
							{
								itemdtos.Add(new I18N_ItemDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_ItemDAO.InsertOrUpdate(itemdtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_ITEM_PARSED), itemdtos.Count,
							region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var mapiddatalist = DAOFactory.I18N_MapIdDataDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var mapiddatadtos = new List<I18N_MapIdDataDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(MapIDDataTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (mapiddatalist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) ==
								null)
							{
								mapiddatadtos.Add(new I18N_MapIdDataDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_MapIdDataDAO.InsertOrUpdate(mapiddatadtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_MAPIDDATA_PARSED),
							mapiddatadtos.Count, region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var mappointdatalist = DAOFactory.I18N_MapPointDataDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var mappointdatadtos = new List<I18N_MapPointDataDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(MapPointDataTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (mappointdatalist.FirstOrDefault(s =>
								s.Key == currentLine[0] && s.RegionType == region) == null)
							{
								mappointdatadtos.Add(new I18N_MapPointDataDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_MapPointDataDAO.InsertOrUpdate(mappointdatadtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_MAPPOINTDATA_PARSED),
							mappointdatadtos.Count, region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var npcmonsterlist = DAOFactory.I18N_NpcMonsterDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var npcmonsterdto = new List<I18N_NpcMonsterDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(MonsterTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (npcmonsterlist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) ==
								null)
							{
								npcmonsterdto.Add(new I18N_NpcMonsterDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_NpcMonsterDAO.InsertOrUpdate(npcmonsterdto);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_MPCMONSTER_PARSED),
							npcmonsterdto.Count, region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var npcmonstertalklist = DAOFactory.I18N_NpcMonsterTalkDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var npctalkdtos = new List<I18N_NpcMonsterTalkDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(NpcTalkTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (npcmonstertalklist.FirstOrDefault(
								s => s.Key == currentLine[0] && s.RegionType == region) == null)
							{
								npctalkdtos.Add(new I18N_NpcMonsterTalkDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_NpcMonsterTalkDAO.InsertOrUpdate(npctalkdtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_NPCMONSTERTALK_PARSED),
							npctalkdtos.Count, region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var questlist = DAOFactory.I18N_QuestDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var questdtos = new List<I18N_QuestDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(QuestTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (questlist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) ==
								null)
							{
								questdtos.Add(new I18N_QuestDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_QuestDAO.InsertOrUpdate(questdtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_QUEST_PARSED), questdtos.Count,
							region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}

			var skilllist = DAOFactory.I18N_SkillDAO.LoadAll().ToList();
			foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
			{
				var skilldtos = new List<I18N_SkillDTO>();
				try
				{
					using (var stream = new StreamReader(I18NTextFileName(SkillTxt, region),
						CodePagesEncodingProvider.Instance.GetEncoding(1252)))
					{
						while ((_line = stream.ReadLine()) != null)
						{
							var currentLine = _line.Split('\t');
							if (skilllist.FirstOrDefault(s => s.Key == currentLine[0] && s.RegionType == region) ==
								null)
							{
								skilldtos.Add(new I18N_SkillDTO
								{
									Key = currentLine[0],
									RegionType = region,
									Text = currentLine[1]
								});
							}
						}

						DAOFactory.I18N_SkillDAO.InsertOrUpdate(skilldtos);

						Logger.Log.Info(string.Format(
							LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_SKILL_PARSED), skilldtos.Count,
							region));
					}
				}
				catch (FileNotFoundException)
				{
				}
			}
		}
	}
}