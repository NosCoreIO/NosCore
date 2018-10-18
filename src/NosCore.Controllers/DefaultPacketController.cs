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

using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data.WebApi;
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NosCore.Shared.Enumerations.Character;
using System.Collections.Concurrent;
using System.Text;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.MapInstanceAccess;

namespace NosCore.Controllers
{
    public class DefaultPacketController : PacketController
    {
        private readonly MapInstanceAccessService _mapInstanceAccessService;
        private readonly WorldConfiguration _worldConfiguration;

        [UsedImplicitly]
        public DefaultPacketController()
        {
        }

        public DefaultPacketController(WorldConfiguration worldConfiguration,
            MapInstanceAccessService mapInstanceAccessService)
        {
            _worldConfiguration = worldConfiguration;
            _mapInstanceAccessService = mapInstanceAccessService;
        }

        public void GameStart([UsedImplicitly] GameStartPacket packet)
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

            Session.Character.SendRelationStatus(true);
            Session.SendPacket(Session.Character.GenerateFinit());
            Session.SendPacket(Session.Character.GenerateBlinit());
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
        public void Preq([UsedImplicitly] PreqPacket packet)
        {
            var currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;
            var timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;
            if (timeSpanSinceLastPortal < 4)
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

            Session.Character.LastPortal = currentRunningSeconds;

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
                    entity = ServerManager.Instance.Sessions.Values
                        .FirstOrDefault(s => s.Character.CharacterId == ncifPacket.TargetId)?.Character;
                    break;
                case VisualType.Monster:
                    entity = Session.Character.MapInstance.Monsters.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Npc:
                    entity = Session.Character.MapInstance.Npcs.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                default:
                    Logger.Log.ErrorFormat(LogLanguage.Instance.GetMessageFromKey(LanguageKey.VISUALTYPE_UNKNOWN), ncifPacket.Type);
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
                && Session.Character.LastSpeedChange.AddSeconds(5) <= DateTime.Now) || distance > 60)
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
            if (guriPacket.Type != 10 || guriPacket.Data < 973 || guriPacket.Data > 999
                || Session.Character.EmoticonsBlocked)
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
            if (pulsePacket.Tick != Session.LastPulse)
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
            const SayColorType type = SayColorType.White;
            Session.Character.MapInstance?.Broadcast(Session, Session.Character.GenerateSay(new SayPacket
            {
                Message = sayPacket.Message,
                Type = type
            }), ReceiverType.AllExceptMeAndBlacklisted);
        }

        /// <summary>
        ///     WhisperPacket
        /// </summary>
        /// <param name="whisperPacket"></param>
        public void WhisperPacket(WhisperPacket whisperPacket)
        {
            try
            {
                var messageBuilder = new StringBuilder();

                //Todo: review this
                var messageData = whisperPacket.Message.Split(' ');
                var receiverName = messageData[whisperPacket.Message.StartsWith("GM ") ? 1 : 0];

                for (var i = messageData[0] == "GM" ? 2 : 1; i < messageData.Length; i++)
                {
                    messageBuilder.Append($"{messageData[i]} ");
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
                    ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character?.Name == receiverName);
                if (receiverSession != null)
                {
                    if (receiverSession.Character.CharacterRelations.Values.Any(s =>
                        s.RelatedCharacterId == Session.Character.CharacterId &&
                        s.RelationType == CharacterRelationType.Blocked))
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                Session.Account.Language),
                        });
                        return;
                    }

                    receiverSession.SendPacket(speakPacket);
                    return;
                }

                ConnectedAccount receiver = null;

                var servers = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels");
                foreach (var server in servers)
                {
                    var accounts = WebApiAccess.Instance
                        .Get<List<ConnectedAccount>>($"api/connectedAccount", server.WebApi);

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

                ServerManager.Instance.BroadcastPacket(new PostedPacket
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
                Logger.Log.Error("Whisper failed.", e);
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
                Logger.Log.Error(Language.Instance.GetMessageFromKey(LanguageKey.USER_IS_NOT_A_FRIEND,
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
                ServerManager.Instance.Sessions.Values.FirstOrDefault(s =>
                    s.Character.CharacterId == btkPacket.CharacterId);

            if (receiverSession != null)
            {
                receiverSession.SendPacket(Session.Character.GenerateTalk(message));
                return;
            }

            ConnectedAccount receiver = null;

            var servers = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels");
            foreach (var server in servers)
            {
                var accounts = WebApiAccess.Instance
                    .Get<List<ConnectedAccount>>($"api/connectedAccount", server.WebApi);

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

            ServerManager.Instance.BroadcastPacket(new PostedPacket
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
                Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_DELETED, Session.Account.Language),
            });
        }

        /// <summary>
        ///     fins packet
        /// </summary>
        /// <param name="finsPacket"></param>
        public void AddFriend(FinsPacket finsPacket)
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
                ServerManager.Instance.Sessions.Values.FirstOrDefault(s =>
                    s.Character.CharacterId == finsPacket.CharacterId);

            if (targetSession == null)
            {
                return;
            }

            if (!targetSession.Character.FriendRequestCharacters.Values.Contains(Session.Character.CharacterId))
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

                    var relation = Session.Character.AddRelation(targetSession.Character.CharacterId,
                        CharacterRelationType.Friend);
                    var targetRelation = targetSession.Character.AddRelation(Session.Character.CharacterId,
                        CharacterRelationType.Friend);

                    Session.Character.RelationWithCharacter.TryAdd(targetRelation.CharacterRelationId, targetRelation);
                    targetSession.Character.RelationWithCharacter.TryAdd(relation.CharacterRelationId, relation);

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
            }
        }

        /// <summary>
        ///     blins packet
        /// </summary>
        /// <param name="blinsPacket"></param>
        public void BlackListAdd(BlInsPacket blinsPacket)
        {
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
                ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.Name == flPacket.CharacterName);

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
                CharacterId = target.Character.CharacterId,
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
            ClientSession target =
                ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.Name == blPacket.CharacterName);

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
                CharacterId = target.Character.CharacterId
            };

            BlackListAdd(blinsPacket);
        }
    }
}