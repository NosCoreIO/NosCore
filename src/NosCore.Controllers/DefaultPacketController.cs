//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.GuriAccess;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Controllers
{
    public class DefaultPacketController : PacketController
    {
        private readonly MapInstanceAccessService _mapInstanceAccessService;
        private readonly GuriAccessService _guriAccessService;
        private readonly WorldConfiguration _worldConfiguration;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [UsedImplicitly]
        public DefaultPacketController()
        {
        }

        public DefaultPacketController(WorldConfiguration worldConfiguration,
            MapInstanceAccessService mapInstanceAccessService,
            GuriAccessService guriAccessService)
        {
            _worldConfiguration = worldConfiguration;
            _mapInstanceAccessService = mapInstanceAccessService;
            _guriAccessService = guriAccessService;
        }

        public void GameStart(GameStartPacket _)
        {
            if (Session.GameStarted || !Session.HasSelectedCharacter)
            {
                // character should have been selected in SelectCharacter
                return;
            }

            Session.GameStarted = true;

            if (_worldConfiguration.SceneOnCreate) // TODO add only first connection check
            {
                Session.SendPacket(new ScenePacket { SceneId = 40 });
            }

            if (_worldConfiguration.WorldInformation)
            {
                Session.SendPacket(Session.Character.GenerateSay("-------------------[NosCore]---------------",
                    SayColorType.Yellow));
                Session.SendPacket(Session.Character.GenerateSay("Github : https://github.com/NosCoreIO/NosCore/",
                    SayColorType.Purple));
                Session.SendPacket(Session.Character.GenerateSay("-----------------------------------------------",
                    SayColorType.Yellow));
            }

            Session.Character.LoadSpeed();
            //            Session.Character.LoadSkills();
            Session.SendPacket(Session.Character.GenerateTit());
            Session.SendPacket(Session.Character.GenerateSpPoint());
            Session.SendPacket(Session.Character.GenerateRsfi());
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
            Session.SendPacket(new PclearPacket());

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

            Session.SendPacket(Session.Character.GenerateGold());
            //            Session.SendPackets(Session.Character.GenerateQuicklist());

            //            string clinit = ServerManager.Instance.TopComplimented.Aggregate("clinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Compliment}|{character.Name}");
            //            string flinit = ServerManager.Instance.TopReputation.Aggregate("flinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Reput}|{character.Name}");
            //            string kdlinit = ServerManager.Instance.TopPoints.Aggregate("kdlinit",
            //                (current, character) => current + $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Act4Points}|{character.Name}");

            //            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());

            Session.Character.SendRelationStatus(true);
            Session.SendPacket(Session.Character.GenerateFinit());
            Session.SendPacket(Session.Character.GenerateBlinit());
            //            Session.SendPacket(clinit);
            //            Session.SendPacket(flinit);
            //            Session.SendPacket(kdlinit);

            //            Session.Character.LastPVPRevive = SystemTime.Now;

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
        /// <param name="_"></param>
        public void Preq(PreqPacket _)
        {
            if ((SystemTime.Now() - Session.Character.LastPortal).TotalSeconds < 4 || Session.Character.LastPortal > Session.Character.LastMove)
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

            if (portal.DestinationMapInstanceId == default)
            {
                return;
            }

            Session.Character.LastPortal = SystemTime.Now();

            if (_mapInstanceAccessService.GetMapInstance(portal.SourceMapInstanceId).MapInstanceType
                != MapInstanceType.BaseMapInstance
                && _mapInstanceAccessService.GetMapInstance(portal.DestinationMapInstanceId).MapInstanceType
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
        ///     ncif packet
        /// </summary>
        /// <param name="ncifPacket"></param>
        public void GetNamedCharacterInformations(NcifPacket ncifPacket)
        {
            IAliveEntity entity;

            switch (ncifPacket.Type)
            {
                case VisualType.Player:
                    entity = Broadcaster.Instance.GetCharacter(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Monster:
                    entity = Session.Character.MapInstance.Monsters.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Npc:
                    entity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), ncifPacket.Type);
                    return;
            }

            Session.SendPacket(entity?.GenerateStatInfo());
        }

        /// <summary>
        ///     Walk Packet
        /// </summary>
        /// <param name="walkPacket"></param>
        public void Walk(WalkPacket walkPacket)
        {
            var distance = (int)Heuristic.Octile(Math.Abs(Session.Character.PositionX - walkPacket.XCoordinate),
                Math.Abs(Session.Character.PositionY - walkPacket.YCoordinate));

            if ((Session.Character.Speed < walkPacket.Speed
                && Session.Character.LastSpeedChange.AddSeconds(5) <= SystemTime.Now()) || distance > 60)
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

            Session.Character.MapInstance?.Sessions.SendPacket(Session.Character.GenerateMove());
            Session.SendPacket(Session.Character.GenerateCond());
            Session.Character.LastMove = SystemTime.Now();
        }

        /// <summary>
        ///     Guri Packet
        /// </summary>
        /// <param name="guriPacket"></param>
        public void Guri(GuriPacket guriPacket)
        {
            _guriAccessService.GuriLaunch(Session, guriPacket);
        }

        public void Pulse(PulsePacket pulsePacket)
        {
            Session.LastPulse += 60;
            if (pulsePacket.Tick != Session.LastPulse)
            {
                Session.Disconnect();
            }
        }

        /// <summary>
        ///     SayPacket
        /// </summary>
        /// <param name="clientSayPacket"></param>
        public void Say(ClientSayPacket clientSayPacket)
        {
            //TODO: Add a penalty check when it will be ready
            const SayColorType type = SayColorType.White;
            Session.Character.MapInstance?.Sessions.SendPacket(Session.Character.GenerateSay(new SayPacket
            {
                Message = clientSayPacket.Message,
                Type = type
            }), new EveryoneBut(Session.Channel.Id)); //TODO  ReceiverType.AllExceptMeAndBlacklisted
        }

        /// <summary>
        ///     WhisperPacket
        /// </summary>
        /// <param name="whisperPacket"></param>
        public void Whisper(WhisperPacket whisperPacket)
        {
            try
            {
                var messageBuilder = new StringBuilder();

                //Todo: review this
                var messageData = whisperPacket.Message.Split(' ');
                var receiverName = messageData[whisperPacket.Message.StartsWith("GM ") ? 1 : 0];

                for (var i = messageData[0] == "GM" ? 2 : 1; i < messageData.Length; i++)
                {
                    messageBuilder.Append(messageData[i]).Append(" ");
                }

                var message = new StringBuilder(messageBuilder.ToString().Length > 60 ? messageBuilder.ToString().Substring(0, 60) : messageBuilder.ToString());

                Session.SendPacket(Session.Character.GenerateSpk(new SpeakPacket
                {
                    SpeakType = SpeakType.Player,
                    Message = message.ToString()
                }));

                var speakPacket = Session.Character.GenerateSpk(new SpeakPacket
                {
                    SpeakType = Session.Account.Authority >= AuthorityType.GameMaster ? SpeakType.GameMaster
                        : SpeakType.Player,
                    Message = message.ToString()
                });

                var receiverSession =
                    Broadcaster.Instance.GetCharacter(s => s.Name == receiverName);
                if (receiverSession != null)
                {
                    if (receiverSession.CharacterRelations.Values.Any(s =>
                        s.RelatedCharacterId == Session.Character.CharacterId
                        && s.RelationType == CharacterRelationType.Blocked))
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                Session.Account.Language)
                        });
                        return;
                    }

                    receiverSession.SendPacket(speakPacket);
                    return;
                }

                ConnectedAccount receiver = null;

                var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)?.Where(c => c.Type == ServerType.WorldServer).ToList();
                foreach (var server in servers ?? new List<ChannelInfo>())
                {
                    var accounts = WebApiAccess.Instance
                        .Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, server.WebApi);

                    if (accounts.Any(a => a.ConnectedCharacter?.Name == receiverName))
                    {
                        receiver = accounts.First(a => a.ConnectedCharacter?.Name == receiverName);
                        break;
                    }
                }

                if (receiver == null)
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        Language.Instance.GetMessageFromKey(LanguageKey.CHARACTER_OFFLINE, Session.Account.Language),
                        SayColorType.Yellow));
                    return;
                }

                if (Session.Character.RelationWithCharacter.Values.Any(s =>
                    s.RelationType == CharacterRelationType.Blocked && s.CharacterId == receiver.ConnectedCharacter.Id))
                {
                    Session.SendPacket(new SayPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                            Session.Account.Language),
                        Type = SayColorType.Yellow
                    });
                    return;
                }

                speakPacket.Message =
                    $"{speakPacket.Message} <{Language.Instance.GetMessageFromKey(LanguageKey.CHANNEL, receiver.Language)}: {MasterClientListSingleton.Instance.ChannelId}>";

                WebApiAccess.Instance.BroadcastPacket(new PostedPacket
                {
                    Packet = PacketFactory.Serialize(new[] { speakPacket }),
                    ReceiverCharacter = new Data.WebApi.Character { Name = receiverName },
                    SenderCharacter = new Data.WebApi.Character { Name = Session.Character.Name },
                    OriginWorldId = MasterClientListSingleton.Instance.ChannelId,
                    ReceiverType = ReceiverType.OnlySomeone
                }, receiver.ChannelId);

                Session.SendPacket(Session.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.SEND_MESSAGE_TO_CHARACTER,
                        Session.Account.Language), SayColorType.Purple));
            }
            catch (Exception e)
            {
                _logger.Error("Whisper failed.", e);
            }
        }

        /// <summary>
        ///     btk packet
        /// </summary>
        /// <param name="btkPacket"></param>
        public void FriendTalk(BtkPacket btkPacket)
        {
            if (!Session.Character.CharacterRelations.Values.Any(s =>
                s.RelatedCharacterId == btkPacket.CharacterId && s.RelationType != CharacterRelationType.Blocked))
            {
                _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.USER_IS_NOT_A_FRIEND,
                    Session.Account.Language));
                return;
            }

            var message = btkPacket.Message;
            if (message.Length > 60)
            {
                message = message.Substring(0, 60);
            }

            message = message.Trim();
            var receiverSession =
                Broadcaster.Instance.GetCharacter(s =>
                    s.VisualId == btkPacket.CharacterId);

            if (receiverSession != null)
            {
                receiverSession.SendPacket(Session.Character.GenerateTalk(message));
                return;
            }

            ConnectedAccount receiver = null;

            var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)?.Where(c => c.Type == ServerType.WorldServer).ToList();
            foreach (var server in servers ?? new List<ChannelInfo>())
            {
                var accounts = WebApiAccess.Instance
                    .Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, server.WebApi);

                if (accounts.Any(a => a.ConnectedCharacter?.Id == btkPacket.CharacterId))
                {
                    receiver = accounts.First(a => a.ConnectedCharacter?.Id == btkPacket.CharacterId);
                }
            }

            if (receiver == null)
            {
                Session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_OFFLINE, Session.Account.Language)
                });
                return;
            }

            WebApiAccess.Instance.BroadcastPacket(new PostedPacket
            {
                Packet = PacketFactory.Serialize(new[] { Session.Character.GenerateTalk(message) }),
                ReceiverCharacter = new Data.WebApi.Character
                { Id = btkPacket.CharacterId, Name = receiver.ConnectedCharacter?.Name },
                SenderCharacter = new Data.WebApi.Character
                { Name = Session.Character.Name, Id = Session.Character.CharacterId },
                OriginWorldId = MasterClientListSingleton.Instance.ChannelId,
                ReceiverType = ReceiverType.OnlySomeone
            }, receiver.ChannelId);
        }

        /// <summary>
        ///     fdel packet
        /// </summary>
        /// <param name="fdelPacket"></param>
        public void DeleteFriend(FdelPacket fdelPacket)
        {
            Session.Character.DeleteRelation(fdelPacket.CharacterId);
            Session.SendPacket(new InfoPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_DELETED, Session.Account.Language)
            });
        }

        /// <summary>
        ///     fins packet
        /// </summary>
        /// <param name="finsPacket"></param>
        public void AddFriend(FinsPacket finsPacket)
        {
            if (_worldConfiguration.FeatureFlags[FeatureFlag.FriendServerEnabled])
            {
                var server = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)?.FirstOrDefault(c => c.Type == ServerType.FriendServer);
                //WebApiAccess.Instance.Post<FriendShip>("api/friend", server.WebApi);
            }
            else
            {
                if (Session.Character.IsFriendListFull)
                {
                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIENDLIST_FULL, Session.Account.Language)
                    });
                    return;
                }

                if (Session.Character.IsRelatedToCharacter(finsPacket.CharacterId, CharacterRelationType.Blocked))
                {
                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                            Session.Account.Language)
                    });
                    return;
                }

                if (Session.Character.IsRelatedToCharacter(finsPacket.CharacterId, CharacterRelationType.Friend))
                {
                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_FRIEND, Session.Account.Language)
                    });
                    return;
                }

                //TODO: Make character options & check if the character has friend requests blocked
                if (Session.Character.FriendRequestBlocked)
                {
                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_BLOCKED,
                            Session.Account.Language)
                    });
                    return;
                }

                var targetSession =
                    Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == finsPacket.CharacterId);

                if (targetSession == null)
                {
                    return;
                }

                if (!targetSession.FriendRequestCharacters.Values.Contains(Session.Character.CharacterId))
                {
                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_SENT,
                            Session.Account.Language)
                    });

                    targetSession.SendPacket(new DlgPacket
                    {
                        Question = string.Format(
                            Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADD, Session.Account.Language),
                            Session.Character.Name),
                        YesPacket = new FinsPacket
                        { Type = FinsPacketType.Accepted, CharacterId = Session.Character.CharacterId },
                        NoPacket = new FinsPacket
                        { Type = FinsPacketType.Rejected, CharacterId = Session.Character.CharacterId }
                    });
                    Session.Character.FriendRequestCharacters[Session.Character.CharacterId] = finsPacket.CharacterId;
                    return;
                }

                switch (finsPacket.Type)
                {
                    case FinsPacketType.Accepted:
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                Session.Account.Language)
                        });

                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                Session.Account.Language)
                        });

                        var relation = Session.Character.AddRelation(targetSession.VisualId,
                            CharacterRelationType.Friend);
                        var targetRelation = targetSession.AddRelation(Session.Character.CharacterId,
                            CharacterRelationType.Friend);

                        Session.Character.RelationWithCharacter.TryAdd(targetRelation.CharacterRelationId, targetRelation);
                        targetSession.RelationWithCharacter.TryAdd(relation.CharacterRelationId, relation);

                        Session.Character.FriendRequestCharacters.TryRemove(Session.Character.CharacterId, out _);
                        break;
                    case FinsPacketType.Rejected:
                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REJECTED,
                                Session.Account.Language)
                        });

                        Session.Character.FriendRequestCharacters.TryRemove(Session.Character.CharacterId, out _);
                        break;
                    default:
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.INVITETYPE_UNKNOWN));
                        break;
                }
            }
        }

        /// <summary>
        ///     blins packet
        /// </summary>
        /// <param name="blinsPacket"></param>
        public void BlackListAdd(BlInsPacket blinsPacket)
        {
            if (Broadcaster.Instance.GetCharacter(s => s.VisualId == blinsPacket.CharacterId) == null)
            {
                _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.USER_NOT_CONNECTED, Session.Account.Language));
                return;
            }

            if (Session.Character.CharacterRelations.Values.Any(s =>
                s.RelatedCharacterId == blinsPacket.CharacterId && s.RelationType != CharacterRelationType.Blocked))
            {
                Session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_BLOCK_FRIEND,
                        Session.Account.Language)
                });
                return;
            }

            if (Session.Character.CharacterRelations.Values.Any(s =>
                s.RelatedCharacterId == blinsPacket.CharacterId && s.RelationType == CharacterRelationType.Blocked))
            {
                Session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_BLACKLISTED,
                        Session.Account.Language)
                });
                return;
            }

            Session.Character.AddRelation(blinsPacket.CharacterId, CharacterRelationType.Blocked);
            Session.SendPacket(new InfoPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_ADDED, Session.Account.Language)
            });
        }

        /// <summary>
        ///     bldel packet
        /// </summary>
        /// <param name="bldelPacket"></param>
        public void BlackListDelete(BlDelPacket bldelPacket)
        {
            if (!Session.Character.CharacterRelations.Values.Any(s =>
                s.RelatedCharacterId == bldelPacket.CharacterId && s.RelationType == CharacterRelationType.Blocked))
            {
                Session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_IN_BLACKLIST,
                        Session.Account.Language)
                });
                return;
            }

            Session.Character.DeleteBlackList(bldelPacket.CharacterId);
        }

        /// <summary>
        ///     flPacket packet
        /// </summary>
        /// <param name="flPacket"></param>
        public void AddDistantFriend(FlPacket flPacket)
        {
            var target =
                Broadcaster.Instance.GetCharacter(s => s.Name == flPacket.CharacterName);

            if (target == null)
            {
                Session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        Session.Account.Language)
                });
                return;
            }

            var fins = new FinsPacket
            {
                CharacterId = target.VisualId,
                Type = FinsPacketType.Accepted
            };

            AddFriend(fins);
        }

        /// <summary>
        ///     blPacket packet
        /// </summary>
        /// <param name="blPacket"></param>
        public void DistantBlackList(BlPacket blPacket)
        {
            var target =
                Broadcaster.Instance.GetCharacter(s => s.Name == blPacket.CharacterName);

            if (target == null)
            {
                Session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        Session.Account.Language)
                });
                return;
            }

            var blinsPacket = new BlInsPacket
            {
                CharacterId = target.VisualId
            };

            BlackListAdd(blinsPacket);
        }

        /// <summary>
        /// rest packet
        /// </summary>
        /// <param name="sitpacket"></param>
        public void Rest(SitPacket sitpacket)
        {
            sitpacket.Users.ForEach(u =>
            {
                IAliveEntity entity;

                switch (u.VisualType)
                {
                    case VisualType.Player:
                        entity = Broadcaster.Instance.GetCharacter(s => s.VisualId == u.VisualId);
                        break;
                    default:
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), u.VisualType);
                        return;
                }
                entity.Rest();
            });
        }
    }
}