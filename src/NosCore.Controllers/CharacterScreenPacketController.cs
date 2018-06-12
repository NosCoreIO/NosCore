using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.ItemInstance;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
	public class CharacterScreenPacketController : PacketController
	{
		/// <summary>
		///     Char_NEW character creation character
		/// </summary>
		/// <param name="characterCreatePacket"></param>
		public void CreateCharacter(CharNewPacket characterCreatePacket)
		{
			if (Session.HasCurrentMapInstance)
			{
				return;
			}

			// TODO: Hold Account Information in Authorized object
			var accountId = Session.Account.AccountId;
			var slot = characterCreatePacket.Slot;
			var characterName = characterCreatePacket.Name;
			if (DAOFactory.CharacterDAO.FirstOrDefault(s =>
				s.AccountId == accountId && s.Slot == slot && s.State == CharacterState.Active) != null)
			{
				return;
			}

			var rg = new Regex(
				@"^[\u0021-\u007E\u00A1-\u00AC\u00AE-\u00FF\u4E00-\u9FA5\u0E01-\u0E3A\u0E3F-\u0E5B\u002E]*$");
			if (rg.Matches(characterName).Count == 1)
			{
				var character =
					DAOFactory.CharacterDAO.FirstOrDefault(s =>
						s.Name == characterName && s.State == CharacterState.Active);
				if (character == null)
				{
					var rnd = new Random();
					CharacterDTO chara = new Character
					{
						Class = (byte) CharacterClassType.Adventurer,
						Gender = characterCreatePacket.Gender,
						HairColor = characterCreatePacket.HairColor,
						HairStyle = characterCreatePacket.HairStyle,
						Hp = 221,
						JobLevel = 1,
						Level = 1,
						MapId = 1,
						MapX = (short) rnd.Next(78, 81),
						MapY = (short) rnd.Next(114, 118),
						Mp = 221,
						MaxMateCount = 10,
						SpPoint = 10000,
						SpAdditionPoint = 0,
						Name = characterName,
						Slot = slot,
						AccountId = accountId,
						MinilandMessage = "Welcome",
						State = CharacterState.Active
					};
					var insertResult = DAOFactory.CharacterDAO.InsertOrUpdate(ref chara);
					LoadCharacters(null);
				}
				else
				{
					Session.SendPacket(new InfoPacket
					{
						Message = Session.GetMessageFromKey(LanguageKey.ALREADY_TAKEN)
					});
				}
			}
			else
			{
				Session.SendPacket(new InfoPacket
				{
					Message = Session.GetMessageFromKey(LanguageKey.INVALID_CHARNAME)
				});
			}
		}

		/// <summary>
		///     Char_DEL packet
		/// </summary>
		/// <param name="characterDeletePacket"></param>
		public void DeleteCharacter(CharacterDeletePacket characterDeletePacket)
		{
			if (Session.HasCurrentMapInstance)
			{
				return;
			}

			var account = DAOFactory.AccountDAO.FirstOrDefault(s => s.AccountId.Equals(Session.Account.AccountId));
			if (account == null)
			{
				return;
			}

			if (account.Password.ToLower() == EncryptionHelper.Sha512(characterDeletePacket.Password))
			{
				var character = DAOFactory.CharacterDAO.FirstOrDefault(s =>
                    s.AccountId == account.AccountId && s.Slot == characterDeletePacket.Slot
                    && s.State == CharacterState.Active);
				if (character == null)
				{
					return;
				}

				character.State = CharacterState.Inactive;
				DAOFactory.CharacterDAO.InsertOrUpdate(ref character);

				LoadCharacters(null);
			}
			else
			{
				Session.SendPacket(new InfoPacket
				{
					Message = Session.GetMessageFromKey(LanguageKey.BAD_PASSWORD)
				});
			}
		}

		/// <summary>
		///     Load Characters, this is the Entrypoint for the Client, Wait for 3 Packets.
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		public void LoadCharacters(EntryPointPacket packet)
		{
            if (Session.Account == null)
			{
				var servers = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels");
				var name = packet.Name;
                var alreadyConnnected = false;
				foreach (var server in servers)
				{
					if (WebApiAccess.Instance.Get<IEnumerable<string>>($"api/connectedAccounts", server.WebApi).Any(a => a == name))
					{
						alreadyConnnected = true;
					}
				}

				if (alreadyConnnected)
				{
					Session.Disconnect();
					return;
                }

                var account = DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == name);

				if (account != null)
				{
					if (account.Password.Equals(EncryptionHelper.Sha512(packet.Password),
						StringComparison.OrdinalIgnoreCase))
					{
						var accountobject = new AccountDTO
						{
							AccountId = account.AccountId,
							Name = account.Name,
							Password = account.Password.ToLower(),
							Authority = account.Authority,
							Language = account.Language
						};
						Session.InitializeAccount(accountobject);
						//Send Account Connected
					}
					else
					{
						Logger.Log.ErrorFormat(LogLanguage.Instance.GetMessageFromKey(LanguageKey.INVALID_PASSWORD));
						Session.Disconnect();
						return;
					}
				}
				else
				{
					Logger.Log.ErrorFormat(LogLanguage.Instance.GetMessageFromKey(LanguageKey.INVALID_ACCOUNT));
					Session.Disconnect();
					return;
				}
			}

			if (Session.Account == null)
			{
				return;
			}

			var characters = DAOFactory.CharacterDAO.Where(s =>
				s.AccountId == Session.Account.AccountId && s.State == CharacterState.Active);
			Logger.Log.InfoFormat(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ACCOUNT_ARRIVED),
				Session.Account.Name);

			// load characterlist packet for each character in Character
			Session.SendPacket(new ClistStartPacket {Type = 0});
			foreach (Character character in characters)
			{
				var equipment = new WearableInstance[16];
				/* IEnumerable<ItemInstanceDTO> inventory = DAOFactory.IteminstanceDAO.Where(s => s.CharacterId == character.CharacterId && s.Type == (byte)InventoryType.Wear);


				 foreach (ItemInstanceDTO equipmentEntry in inventory)
				 {
				     // explicit load of iteminstance
				     WearableInstance currentInstance = equipmentEntry as WearableInstance;
				     equipment[(short)currentInstance.Item.EquipmentSlot] = currentInstance;

				 }
				    */
				var petlist = new List<short?>();
				var mates = DAOFactory.MateDAO.Where(s => s.CharacterId == character.CharacterId).ToList();
				for (var i = 0; i < 26; i++)
				{
					if (mates.Count > i)
					{
						petlist.Add(mates[i].Skin);
						petlist.Add(mates[i].VNum);
					}
					else
					{
						petlist.Add(-1);
					}
				}

				// 1 1 before long string of -1.-1 = act completion
				Session.SendPacket(new ClistPacket
				{
					Slot = character.Slot,
					Name = character.Name,
					Unknown = 0,
					Gender = (byte) character.Gender,
					HairStyle = (byte) character.HairStyle,
					HairColor = (byte) character.HairColor,
					Unknown1 = 0,
					Class = (CharacterClassType) character.Class,
					Level = character.Level,
					HeroLevel = character.HeroLevel,
					Equipments = new List<short?>
					{
						equipment[(byte) EquipmentType.Hat]?.VNum ?? -1,
						equipment[(byte) EquipmentType.Armor]?.VNum ?? -1,
						equipment[(byte) EquipmentType.WeaponSkin]?.VNum ??
						(equipment[(byte) EquipmentType.MainWeapon]?.VNum ?? -1),
						equipment[(byte) EquipmentType.SecondaryWeapon]?.VNum ?? -1,
						equipment[(byte) EquipmentType.Mask]?.VNum ?? -1,
						equipment[(byte) EquipmentType.Fairy]?.VNum ?? -1,
						equipment[(byte) EquipmentType.CostumeSuit]?.VNum ?? -1,
						equipment[(byte) EquipmentType.CostumeHat]?.VNum ?? -1
					},
					JobLevel = character.JobLevel,
					QuestCompletion = 1,
					QuestPart = 1,
					Pets = petlist,
					Design = equipment[(byte) EquipmentType.Hat]?.Item.IsColored == true
						? equipment[(byte) EquipmentType.Hat].Design : 0,
					Unknown3 = 0
				});
			}

			Session.SendPacket(new ClistEndPacket());
		}

		public void SelectCharacter(SelectPacket selectPacket)
		{
			try
			{
				if (Session?.Account == null || Session.HasSelectedCharacter)
				{
					return;
				}

				var characterDto =
					DAOFactory.CharacterDAO.FirstOrDefault(s =>
                        s.AccountId == Session.Account.AccountId && s.Slot == selectPacket.Slot
                        && s.State == CharacterState.Active);
				if (characterDto == null)
				{
					return;
				}

				if (!(characterDto is Character character))
				{
					return;
				}

				character.MapInstanceId = ServerManager.Instance.GetBaseMapInstanceIdByMapId(character.MapId);
				character.MapInstance = ServerManager.Instance.GetMapInstance(character.MapInstanceId);
				character.PositionX = character.MapX;
				character.PositionY = character.MapY;
				character.Account = Session.Account;
				Session.SetCharacter(character);
				if (Session.Character.Hp > Session.Character.HPLoad())
				{
					Session.Character.Hp = (int) Session.Character.HPLoad();
				}

				if (Session.Character.Mp > Session.Character.MPLoad())
				{
					Session.Character.Mp = (int) Session.Character.MPLoad();
				}

				Session.SendPacket(new OKPacket());
			}
			catch (Exception ex)
			{
				Logger.Log.Error("Select character failed.", ex);
			}
		}
	}
}