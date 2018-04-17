using NosCore.Core.Serializing.HandlerSerialization;
using NosCore.Domain.Map;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using System;
using System.Diagnostics;

namespace NosCore.Handler
{
    public class DefaultPacketHandler : IPacketHandler
    {
#region Members

        #endregion

        #region Instantiation
        public DefaultPacketHandler()
        { }
        public DefaultPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void GameStart(GameStartPacket packet)
        {
            if (Session.IsOnMap || !Session.HasSelectedCharacter)
            {
                // character should have been selected in SelectCharacter
                return;
            }

            Session.CurrentMapInstance = Session.Character.MapInstance;

            //if (ConfigurationManager.AppSettings["SceneOnCreate"].ToLower() == "true" & Session.Character.GeneralLogs.Count(s => s.LogType == "Connection") < 2)
            {
                //Session.SendPacket("scene 40");
            }
            //if (ConfigurationManager.AppSettings["WorldInformation"].ToLower() == "true")
            {
                Session.SendPacket(Session.Character.GenerateSay("-------------------[NosCore]---------------", 10));
                Session.SendPacket(Session.Character.GenerateSay($"Github : https://github.com/NosCoreIO/NosCore/", 11));
                Session.SendPacket(Session.Character.GenerateSay("-----------------------------------------------", 10));
            }
            //            Session.Character.LoadSpeed();
            //            Session.Character.LoadSkills();
            //            Session.SendPacket(Session.Character.GenerateTit());
            //            Session.SendPacket(Session.Character.GenerateSpPoint());
            //            Session.SendPacket("rsfi 1 1 0 9 0 9");
            if (Session.Character.Hp <= 0)
            {
                //                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
            }
            else
            {
                Session.ChangeMap();
            }
            //            Session.SendPacket(Session.Character.GenerateSki());
            //            Session.SendPacket($"fd {Session.Character.Reput} 0 {(int)Session.Character.Dignity} {Math.Abs(Session.Character.GetDignityIco())}");
            Session.SendPacket(Session.Character.GenerateFd());
            //            Session.SendPacket("rage 0 250000");
            //            Session.SendPacket("rank_cool 0 0 18000");
            //            SpecialistInstance specialistInstance = Session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>(8, InventoryType.Wear);
            //            StaticBonusDTO medal = Session.Character.StaticBonusList.FirstOrDefault(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            //            if (medal != null)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOGIN_MEDAL"), 12));
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
            //            Session.SendPacket(UserInterfaceHelper.Instance.GeneratePClear());

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

            //            Session.SendPacket(Session.Character.GenerateGold());
            //            Session.SendPackets(Session.Character.GenerateQuicklist());

            //            string clinit = ServerManager.Instance.TopComplimented.Aggregate("clinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Compliment}|{character.Name}");
            //            string flinit = ServerManager.Instance.TopReputation.Aggregate("flinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Reput}|{character.Name}");
            //            string kdlinit = ServerManager.Instance.TopPoints.Aggregate("kdlinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Act4Points}|{character.Name}");

            //            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());

            //            Session.SendPacket(Session.Character.GenerateFinit());
            //            Session.SendPacket(Session.Character.GenerateBlinit());
            //            Session.SendPacket(clinit);
            //            Session.SendPacket(flinit);
            //            Session.SendPacket(kdlinit);

            //            Session.Character.LastPVPRevive = DateTime.Now;

            //            long? familyId = DAOFactory.FamilyCharacterDAO.FirstOrDefault(s => s.CharacterId == Session.Character.CharacterId)?.FamilyId;
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

            //            IEnumerable<PenaltyLogDTO> warning = DAOFactory.PenaltyLogDAO.Where(s => s.AccountId == Session.Character.AccountId).Where(p => p.Penalty == PenaltyType.Warning);
            //            IEnumerable<PenaltyLogDTO> penaltyLogDtos = warning as IList<PenaltyLogDTO> ?? warning.ToList();
            //            if (penaltyLogDtos.Any())
            //            {
            //                Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("WARNING_INFO"), penaltyLogDtos.Count())));
            //            }

            //            // finfo - friends info
            //            IEnumerable<MailDTO> mails = DAOFactory.MailDAO.Where(s => s.ReceiverId.Equals(Session.Character.CharacterId)).ToList();

            //            foreach (MailDTO mail in mails)
            //            {
            //                Session.Character.GenerateMail(mail);
            //            }
            //            int giftcount = mails.Count(mail => !mail.IsSenderCopy && mail.ReceiverId == Session.Character.CharacterId && mail.AttachmentVNum != null && !mail.IsOpened);
            //            int mailcount = mails.Count(mail => !mail.IsSenderCopy && mail.ReceiverId == Session.Character.CharacterId && mail.AttachmentVNum == null && !mail.IsOpened);
            //            if (giftcount > 0)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("GIFTED"), giftcount), 11));
            //            }
            //            if (mailcount > 0)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NEW_MAIL"), mailcount), 10));
            //            }
            //            Session.Character.DeleteTimeout();

            //            foreach (StaticBuffDTO sb in DAOFactory.StaticBuffDAO.Where(s => s.CharacterId == Session.Character.CharacterId))
            //            {
            //                Session.Character.AddStaticBuff(sb);
            //            }
            //            if (Session.Character.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act4 || m.MapTypeId == (short)MapTypeEnum.Act42))
            //            {
            //                Session.Character.ConnectAct4();
            //            }
        }

        /// <summary>
        /// Walk Packet
        /// </summary>
        /// <param name="walkPacket"></param>
        public void Walk(WalkPacket walkPacket)
        {
            double currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
            int distance = Maps.GetDistance(Session.Character.PositionX, Session.Character.PositionY,
               walkPacket.XCoordinate, walkPacket.YCoordinate);

            if ((Session.Character.Speed >= walkPacket.Speed || Session.Character.LastSpeedChange.AddSeconds(5) > DateTime.Now) && !(distance > 60))
            {
                if (Session.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    Session.Character.MapX = walkPacket.XCoordinate;
                    Session.Character.MapY = walkPacket.YCoordinate;
                }
                Session.Character.PositionX = walkPacket.XCoordinate;
                Session.Character.PositionY = walkPacket.YCoordinate;


                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateMove());
                Session.SendPacket(Session.Character.GenerateCond());
                Session.Character.LastMove = DateTime.Now;
            }
        }

        /// <summary>
        /// Guri Packet
        /// </summary>
        /// <param name="guriPacket"></param>
        public void Guri(GuriPacket guriPacket)
        {
            
        }
        #endregion
    }
}
