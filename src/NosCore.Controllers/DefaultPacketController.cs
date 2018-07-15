using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core.Networking;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
	public class DefaultPacketController : PacketController
	{
		private readonly WorldConfiguration _worldConfiguration;

		[UsedImplicitly]
        public DefaultPacketController()
		{
		}

		public DefaultPacketController(WorldConfiguration worldConfiguration)
		{
			_worldConfiguration = worldConfiguration;
		}

		public void GameStart([UsedImplicitly]GameStartPacket packet)
		{
			if (Session.GameStarted || !Session.HasSelectedCharacter)
			{
				// character should have been selected in SelectCharacter
				return;
			}

			Session.GameStarted = true;

			if (_worldConfiguration.SceneOnCreate) // TODO add only first connection check
			{
				Session.SendPacket(new ScenePacket {SceneId = 40});
			}

			if (_worldConfiguration.WorldInformation)
			{
				Session.SendPacket(Session.Character.GenerateSay("-------------------[NosCore]---------------",
					SayColorType.Yellow));
				Session.SendPacket(Session.Character.GenerateSay($"Github : https://github.com/NosCoreIO/NosCore/",
					SayColorType.Purple));
				Session.SendPacket(Session.Character.GenerateSay("-----------------------------------------------",
					SayColorType.Yellow));
			}

			Session.Character.LoadSpeed();
			//            Session.Character.LoadSkills();
			Session.SendPacket(Session.Character.GenerateTit());
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
			Session.SendPacket(Session.Character.GenerateStat());
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
			//                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("GIFTED"), giftcount), SayColorType.Purple));
			//            }
			//            if (mailcount > 0)
			//            {
			//                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NEW_MAIL"), mailcount), SayColorType.Yellow));
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
		///     PreqPacket packet
		/// </summary>
		/// <param name="packet"></param>
		public void Preq([UsedImplicitly]PreqPacket packet)
		{
			var currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;
			var timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;
			if (!(timeSpanSinceLastPortal >= 4))
			{
				return;
			}

			var portal = Session.Character.MapInstance.Portals.Find(port =>
				Heuristic.Octile(Math.Abs(Session.Character.PositionX - port.SourceX),
					Math.Abs(Session.Character.PositionY - port.SourceY)) <= 1);
			if (portal == null)
			{
				return;
			}

			if (portal.DestinationMapInstanceId == default(Guid))
			{
				return;
			}

			Session.Character.LastPortal = currentRunningSeconds;

			if (ServerManager.Instance.GetMapInstance(portal.SourceMapInstanceId).MapInstanceType
				!= MapInstanceType.BaseMapInstance
				&& ServerManager.Instance.GetMapInstance(portal.DestinationMapInstanceId).MapInstanceType
				== MapInstanceType.BaseMapInstance)
			{
				Session.ChangeMap(Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
			}
			else
			{
				Session.ChangeMapInstance(portal.DestinationMapInstanceId, portal.DestinationX,
					portal.DestinationY);
			}
		}

		/// <summary>
		///     Walk Packet
		/// </summary>
		/// <param name="walkPacket"></param>
		public void Walk(WalkPacket walkPacket)
		{
			var currentRunningSeconds =
				(DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
			var distance = (int) Heuristic.Octile(Math.Abs(Session.Character.PositionX - walkPacket.XCoordinate),
				Math.Abs(Session.Character.PositionY - walkPacket.YCoordinate));

			if ((Session.Character.Speed < walkPacket.Speed &&
				Session.Character.LastSpeedChange.AddSeconds(5) <= DateTime.Now) || distance > 60)
			{
				return;
			}

			if (Session.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
			{
				Session.Character.MapX = walkPacket.XCoordinate;
				Session.Character.MapY = walkPacket.YCoordinate;
			}

			Session.Character.PositionX = walkPacket.XCoordinate;
			Session.Character.PositionY = walkPacket.YCoordinate;

			Session.Character.MapInstance?.Broadcast(Session.Character.GenerateMove());
			Session.SendPacket(Session.Character.GenerateCond());
			Session.Character.LastMove = DateTime.Now;
		}

		/// <summary>
		///     Guri Packet
		/// </summary>
		/// <param name="guriPacket"></param>
		public void Guri(GuriPacket guriPacket)
		{
			if (guriPacket.Type != 10 || guriPacket.Data < 973 || guriPacket.Data > 999 ||
				Session.Character.EmoticonsBlocked)
			{
				return;
			}

			if (guriPacket.VisualEntityId != null
				&& Convert.ToInt64(guriPacket.VisualEntityId.Value) == Session.Character.CharacterId)
			{
				Session.Character.MapInstance.Broadcast(Session,
					Session.Character.GenerateEff(guriPacket.Data + 4099), ReceiverType.AllNoEmoBlocked);
			}
		}

        public void Pulse(PulsePacket pulsePacket)
        {
            Session.LastPulse += 60;
            if(pulsePacket.Tick != Session.LastPulse)
            {
                Session.Disconnect();
            }
        }

	    /// <summary>
	    ///     SayPacket
	    /// </summary>
	    /// <param name="sayPacket"></param>
	    public void SayPacket(ClientSayPacket sayPacket)
	    {
            //TODO: Add a penalty check when it will be ready
	        var type = SayColorType.White;
            Session.Character.MapInstance?.Broadcast(Session, Session.Character.GenerateSay(new SayPacket
            {
                Message = sayPacket.Message,
                Type = type
            } ), ReceiverType.AllExceptMe);
	    }

	    /// <summary>
	    ///     WhisperPacket
	    /// </summary>
	    /// <param name="whisperPacket"></param>
	    public void WhisperPacket(WhisperPacket whisperPacket)
	    {
	        try
	        {
	            string message = string.Empty;
	            if (string.IsNullOrEmpty(whisperPacket.Message))
	            {
	                return;
	            }

                //Todo: review this
	            string[] messageData = whisperPacket.Message.Split(' ');
                string receiverName = messageData[whisperPacket.Message.StartsWith("GM ") ? 1 : 0];

	            for (int i = messageData[0] == "GM" ? 2 : 1; i < messageData.Length; i++)
	            {
	                message += $"{messageData[i]} ";
	            }

	            message = whisperPacket.Message.Length > 60 ? whisperPacket.Message.Substring(0, 60) : message;
	            message = message.Trim();

                Session.SendPacket(Session.Character.GenerateSpk(new SpeakPacket
                {
                    SpeakType = SpeakType.Player,
                    Message = message
                }));

	            CharacterDTO receiver = DAOFactory.CharacterDAO.FirstOrDefault(s => s.Name == receiverName);

	            if (receiver == null)
	            {
	                return;
	            }
                //Todo: Add a check for blacklisted characters when the CharacterRelation system will be done
	        }
	        catch (Exception e)
	        {
                Logger.Log.Error("Whisper failed.", e);
	        }
	    }
    }
}
