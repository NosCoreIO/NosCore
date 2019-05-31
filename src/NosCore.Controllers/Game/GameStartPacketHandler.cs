using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ServerPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Game
{
    public class GameStartPacketHandler : PacketHandler<GameStartPacket>, IWorldPacketHandler
    {
        private readonly WorldConfiguration _worldConfiguration;
        private readonly IWebApiAccess _webApiAccess;
        private readonly ISerializer _packetSerializer;

        public GameStartPacketHandler(WorldConfiguration worldConfiguration, IWebApiAccess webApiAccess, ISerializer packetSerializer)
        {
            _worldConfiguration = worldConfiguration;
            _webApiAccess = webApiAccess;
            _packetSerializer = packetSerializer;
        }

        public override void Execute(GameStartPacket _, ClientSession session)
        {

            if (session.GameStarted || !session.HasSelectedCharacter)
            {
                // character should have been selected in SelectCharacter
                return;
            }

            session.GameStarted = true;

            if (_worldConfiguration.SceneOnCreate) // TODO add only first connection check
            {
                session.SendPacket(new ScenePacket { SceneId = 40 });
            }

            if (_worldConfiguration.WorldInformation)
            {
                session.SendPacket(session.Character.GenerateSay("-------------------[NosCore]---------------",
                    SayColorType.Yellow));
                session.SendPacket(session.Character.GenerateSay("Github : https://github.com/NosCoreIO/NosCore/",
                    SayColorType.Purple));
                session.SendPacket(session.Character.GenerateSay("-----------------------------------------------",
                    SayColorType.Yellow));
            }

            session.Character.LoadSpeed();
            //            Session.Character.LoadSkills();
            session.SendPacket(session.Character.GenerateTit());
            session.SendPacket(session.Character.GenerateSpPoint());
            session.SendPacket(session.Character.GenerateRsfi());
            if (session.Character.Hp <= 0)
            {
                //                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
            }
            else
            {
                session.ChangeMap();
            }

            //            Session.SendPacket(Session.Character.GenerateSki());
            //            Session.SendPacket($"fd {Session.Character.Reput} 0 {(int)Session.Character.Dignity} {Math.Abs(Session.Character.GetDignityIco())}");
            session.SendPacket(session.Character.GenerateFd());
            session.SendPacket(session.Character.GenerateStat());
            //            Session.SendPacket("rage 0 250000");
            //            Session.SendPacket("rank_cool 0 0 18000");
            //            SpecialistInstance specialistInstance = Session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>(8, InventoryType.Wear);
            //            StaticBonusDTO medal = Session.Character.StaticBonusList.FirstOrDefault(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            //            if (medal != null)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOGIN_MEDAL"), SayColorType.Green));
            //            }

            //            if (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBasket))
            //            {
            //                Session.SendPacket("ib 1278 1");
            //            }

            //            if (Session.Character.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.CleftOfDarkness))
            //            {
            //                Session.SendPacket("bc 0 0 0");
            //            }
            //            if (specialistInstance != null)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSpPoint());
            //            }
            //            Session.SendPacket("scr 0 0 0 0 0 0");
            //            for (int i = 0; i < 10; i++)
            //            {
            //                Session.SendPacket($"bn {i} {Language.Instance.GetMessageFromKey($"BN{i}")}");
            //            }
            //            Session.SendPacket(Session.Character.GenerateExts());
            //            Session.SendPacket(Session.Character.GenerateMlinfo());
            session.SendPacket(new PclearPacket());

            //            Session.SendPacket(Session.Character.GeneratePinit());
            //            Session.SendPackets(Session.Character.GeneratePst());

            //            Session.SendPacket("zzim");
            //            Session.SendPacket($"twk 2 {Session.Character.CharacterId} {Session.Account.Name} {Session.Character.Name} shtmxpdlfeoqkr");

            //            // qstlist target sqst bf
            //            Session.SendPacket("act6");
            //            Session.SendPacket(Session.Character.GenerateFaction());
            //            // MATES
            //            Session.SendPackets(Session.Character.GenerateScP());
            //            Session.SendPackets(Session.Character.GenerateScN());
            //            Session.Character.GenerateStartupInventory();

            session.SendPacket(session.Character.GenerateGold());
            session.SendPacket(session.Character.GenerateCond());
            //            Session.SendPackets(Session.Character.GenerateQuicklist());

            //            string clinit = ServerManager.Instance.TopComplimented.Aggregate("clinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Compliment}|{character.Name}");
            //            string flinit = ServerManager.Instance.TopReputation.Aggregate("flinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Reput}|{character.Name}");
            //            string kdlinit = ServerManager.Instance.TopPoints.Aggregate("kdlinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Act4Points}|{character.Name}");

            //            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());

            var friendlist = _webApiAccess.Get<List<CharacterRelationStatus>>(WebApiRoute.Friend, session.Character.CharacterId) ?? new List<CharacterRelationStatus>();
            foreach(var friend in friendlist)
            {
                _webApiAccess.BroadcastPacket(new PostedPacket
                {
                    Packet = _packetSerializer.Serialize(new[]
                   {
                            new FinfoPacket
                            {
                                FriendList = new List<FinfoSubPackets>
                                {
                                    new FinfoSubPackets
                                    {
                                        CharacterId = friend.CharacterId,
                                        IsConnected = true
                                    }
                                }
                            }
                        }),
                    ReceiverType = ReceiverType.OnlySomeone,
                    SenderCharacter = new Data.WebApi.Character { Id = session.Character.CharacterId, Name = session.Character.Name },
                    ReceiverCharacter = new Data.WebApi.Character
                    {
                        Id = friend.CharacterId,
                        Name = friend.CharacterName
                    }
                });
            }

            session.SendPacket(session.Character.GenerateFinit(_webApiAccess));
            session.SendPacket(session.Character.GenerateBlinit(_webApiAccess));
            //            Session.SendPacket(clinit);
            //            Session.SendPacket(flinit);
            //            Session.SendPacket(kdlinit);

            //            Session.Character.LastPVPRevive = SystemTime.Now;

            //            long? familyId = _familyCharacterDAO.FirstOrDefault(s => s.CharacterId == Session.Character.CharacterId)?.FamilyId;
            //            if (familyId != null)
            //            {
            //                Session.Character.Family = ServerManager.Instance.FamilyList.FirstOrDefault(s => s.FamilyId == familyId.Value);
            //            }

            //            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
            //            {
            //                Session.SendPacket(Session.Character.GenerateGInfo());
            //                Session.SendPackets(Session.Character.GetFamilyHistory());
            //                Session.SendPacket(Session.Character.GenerateFamilyMember());
            //                Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
            //                Session.SendPacket(Session.Character.GenerateFamilyMemberExp());
            //                if (!string.IsNullOrWhiteSpace(Session.Character.Family.FamilyMessage))
            //                {
            //                    Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo("--- Family Message ---\n" + Session.Character.Family.FamilyMessage));
            //                }
            //            }

            //            IEnumerable<PenaltyLogDTO> warning = _penaltyDao.Where(s => s.AccountId == Session.Character.AccountId).Where(p => p.Penalty == PenaltyType.Warning);
            //            IEnumerable<PenaltyLogDTO> penaltyLogDtos = warning as IList<PenaltyLogDTO> ?? warning.ToList();
            //            if (penaltyLogDtos.Any())
            //            {
            //                Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("WARNING_INFO"), penaltyLogDtos.Count())));
            //            }

            //            // finfo - friends info
            //            IEnumerable<MailDTO> mails = _mailDao.Where(s => s.ReceiverId.Equals(Session.Character.CharacterId)).ToList();

            //            foreach (MailDTO mail in mails)
            //            {
            //                Session.Character.GenerateMail(mail);
            //            }
            //            int giftcount = mails.Count(mail => !mail.IsSenderCopy && mail.ReceiverId == Session.Character.CharacterId && mail.AttachmentVNum != null && !mail.IsOpened);
            //            int mailcount = mails.Count(mail => !mail.IsSenderCopy && mail.ReceiverId == Session.Character.CharacterId && mail.AttachmentVNum == null && !mail.IsOpened);
            //            if (giftcount > 0)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("GIFTED"), giftcount), SayColorType.Purple));
            //            }
            //            if (mailcount > 0)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NEW_MAIL"), mailcount), SayColorType.Yellow));
            //            }
            //            Session.Character.DeleteTimeout();

            //            foreach (StaticBuffDTO sb in _staticBuffDao.Where(s => s.CharacterId == Session.Character.CharacterId))
            //            {
            //                Session.Character.AddStaticBuff(sb);
            //            }
            //            if (Session.Character.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act4 || m.MapTypeId == (short)MapTypeEnum.Act42))
            //            {
            //                Session.Character.ConnectAct4();
            //            }
        }
    }
}
