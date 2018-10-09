using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.WebApi;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Item;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Mapster;
using System.Collections.Concurrent;
using NosCore.GameObject.Services;
using NosCore.GameObject.Services.MapInstanceAccess;

namespace NosCore.Controllers
{
    public class CharacterScreenPacketController : PacketController
    {
        private readonly MapInstanceAccessService _mapInstanceAccessService;
        private readonly ICharacterBuilderService _characterBuilderService;
        private readonly IItemBuilderService _itemBuilderService;

        public CharacterScreenPacketController(ICharacterBuilderService characterBuilderService,
            IItemBuilderService itemBuilderService, MapInstanceAccessService mapInstanceAccessService)
        {
            _mapInstanceAccessService = mapInstanceAccessService;
            _characterBuilderService = characterBuilderService;
            _itemBuilderService = itemBuilderService;
        }

        [UsedImplicitly]
        public CharacterScreenPacketController()
        {
        }
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
                    CharacterDTO chara = new CharacterDTO
                    {
                        Class = (byte)CharacterClassType.Adventurer,
                        Gender = characterCreatePacket.Gender,
                        HairColor = characterCreatePacket.HairColor,
                        HairStyle = characterCreatePacket.HairStyle,
                        Hp = 221,
                        JobLevel = 1,
                        Level = 1,
                        MapId = 1,
                        MapX = (short)RandomFactory.Instance.RandomNumber(78, 81),
                        MapY = (short)RandomFactory.Instance.RandomNumber(114, 118),
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
                    DAOFactory.CharacterDAO.InsertOrUpdate(ref chara);
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
                    if (WebApiAccess.Instance
                        .Get<List<ConnectedAccount>>($"api/connectedAccount", server.WebApi)
                        .Any(a => a.Name == name))
                    {
                        alreadyConnnected = true;
                        break;
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
                        SessionFactory.Instance.Sessions.FirstOrDefault(s=>s.Value.SessionId == Session.SessionId).Value.RegionType = account.Language;
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
            Session.SendPacket(new ClistStartPacket { Type = 0 });
            foreach (GameObject.Character character in characters.Select(_characterBuilderService.LoadCharacter))
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
                    Gender = (byte)character.Gender,
                    HairStyle = (byte)character.HairStyle,
                    HairColor = (byte)character.HairColor,
                    Unknown1 = 0,
                    Class = (CharacterClassType)character.Class,
                    Level = character.Level,
                    HeroLevel = character.HeroLevel,
                    Equipments = new List<short?>
                    {
                        equipment[(byte) EquipmentType.Hat]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.Armor]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.WeaponSkin]?.ItemVNum ??
                        (equipment[(byte) EquipmentType.MainWeapon]?.ItemVNum ?? -1),
                        equipment[(byte) EquipmentType.SecondaryWeapon]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.Mask]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.Fairy]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.CostumeSuit]?.ItemVNum ?? -1,
                        equipment[(byte) EquipmentType.CostumeHat]?.ItemVNum ?? -1
                    },
                    JobLevel = character.JobLevel,
                    QuestCompletion = 1,
                    QuestPart = 1,
                    Pets = petlist,
                    Design = equipment[(byte)EquipmentType.Hat]?.Item.IsColored == true
                        ? equipment[(byte)EquipmentType.Hat].Design : 0,
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

                GameObject.Character character = _characterBuilderService.LoadCharacter(characterDto);

                character.MapInstanceId = _mapInstanceAccessService.GetBaseMapInstanceIdByMapId(character.MapId);
                character.MapInstance = _mapInstanceAccessService.GetMapInstance(character.MapInstanceId);
                character.PositionX = character.MapX;
                character.PositionY = character.MapY;
                character.Account = Session.Account;
                Session.SetCharacter(character);

                var inventories = DAOFactory.ItemInstanceDAO.Where(s => s.CharacterId == character.CharacterId).ToList();
                inventories.ForEach(k => character.Inventory[k.Id] = _itemBuilderService.Convert(k));
#pragma warning disable CS0618
                Session.SendPackets(Session.Character.GenerateInv());
#pragma warning restore CS0618

                if (Session.Character.Hp > Session.Character.HPLoad())
                {
                    Session.Character.Hp = (int)Session.Character.HPLoad();
                }

                if (Session.Character.Mp > Session.Character.MPLoad())
                {
                    Session.Character.Mp = (int)Session.Character.MPLoad();
                }

                IEnumerable<CharacterRelation> relations = DAOFactory.CharacterRelationDAO.Where(s => s.CharacterId == Session.Character.CharacterId).Cast<CharacterRelation>();
                IEnumerable<CharacterRelation> relationsWithCharacter = DAOFactory.CharacterRelationDAO.Where(s => s.RelatedCharacterId == Session.Character.CharacterId).Cast<CharacterRelation>();

                List<CharacterDTO> characters = DAOFactory.CharacterDAO.Where(s => relations.Select(v => v.RelatedCharacterId).Contains(s.CharacterId)).ToList();
                List<CharacterDTO> relatedCharacters = DAOFactory.CharacterDAO.Where(s => relationsWithCharacter.Select(v => v.RelatedCharacterId).Contains(s.CharacterId)).ToList();

                foreach (CharacterRelation relation in relations)
                {
                    relation.CharacterName = characters.FirstOrDefault(s => s.CharacterId == relation.RelatedCharacterId)?.Name;
                    Session.Character.CharacterRelations[relation.CharacterRelationId] = relation;
                }

                foreach (CharacterRelation relation in relationsWithCharacter)
                {
                    relation.CharacterName = relatedCharacters.FirstOrDefault(s => s.CharacterId == relation.RelatedCharacterId)?.Name;
                    Session.Character.RelationWithCharacter[relation.CharacterRelationId] = relation;
                }

                Session.SendPacket(new OkPacket());
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Select character failed.", ex);
            }
        }
    }
}