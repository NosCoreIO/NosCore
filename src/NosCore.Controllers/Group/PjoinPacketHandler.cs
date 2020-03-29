//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
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
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using Serilog;

namespace NosCore.PacketHandlers.Group
{
    public class PjoinPacketHandler : PacketHandler<PjoinPacket>, IWorldPacketHandler
    {
        private readonly IBlacklistHttpClient _blacklistHttpCLient;
        private readonly ILogger _logger;

        public PjoinPacketHandler(ILogger logger, IBlacklistHttpClient blacklistHttpCLient)
        {
            _logger = logger;
            _blacklistHttpCLient = blacklistHttpCLient;
        }

        public override async Task Execute(PjoinPacket pjoinPacket, ClientSession clientSession)
        {
            var targetSession =
                Broadcaster.Instance.GetCharacter(s =>
                    s.VisualId == pjoinPacket.CharacterId);

            if ((targetSession == null) && (pjoinPacket.RequestType != GroupRequestType.Sharing))
            {
                _logger.Error(GameLanguage.Instance.GetMessageFromKey(LanguageKey.UNABLE_TO_REQUEST_GROUP,
                    clientSession.Account.Language));
                return;
            }

            switch (pjoinPacket.RequestType)
            {
                case GroupRequestType.Requested:
                case GroupRequestType.Invited:
                    if (pjoinPacket.CharacterId == clientSession.Character.CharacterId)
                    {
                        return;
                    }

                    if (targetSession?.Group?.IsGroupFull ?? true)
                    {
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);
                        return;
                    }

                    if ((targetSession.Group!.Count > 1) && (clientSession.Character.Group!.Count > 1))
                    {
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.ALREADY_IN_GROUP,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);
                        return;
                    }

                    var blacklisteds = await _blacklistHttpCLient.GetBlackLists(clientSession.Character.VisualId).ConfigureAwait(false);
                    if (blacklisteds != null && blacklisteds.Any(s => s.CharacterId == pjoinPacket.CharacterId))
                    {
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (targetSession.GroupRequestBlocked)
                    {
                        await clientSession.SendPacket(new MsgPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_BLOCKED,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (clientSession.Character.LastGroupRequest != null)
                    {
                        TimeSpan diffTimeSpan = ((DateTime)clientSession.Character.LastGroupRequest).AddSeconds(5) - SystemTime.Now();
                        if (diffTimeSpan.Seconds > 0 && diffTimeSpan.Seconds <= 5)
                        {
                            await clientSession.SendPacket(new InfoPacket
                            {
                                Message = string.Format(
                                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.DELAY_GROUP_REQUEST,
                                        clientSession.Account.Language), diffTimeSpan.Seconds)
                            }).ConfigureAwait(false);
                            return;
                        }
                    }

                    clientSession.Character.GroupRequestCharacterIds.TryAdd(pjoinPacket.CharacterId, pjoinPacket.CharacterId);
                    clientSession.Character.LastGroupRequest = SystemTime.Now();

                    if (((clientSession.Character.Group!.Count == 1) ||
                            (clientSession.Character.Group.Type == GroupType.Group))
                        && ((targetSession.Group.Count == 1) || (targetSession?.Group.Type == GroupType.Group)))
                    {
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = clientSession.GetMessageFromKey(LanguageKey.GROUP_INVITE)
                        }).ConfigureAwait(false);
                        await targetSession.SendPacket(new DlgPacket
                        {
                            Question = string.Format(
                                GameLanguage.Instance.GetMessageFromKey(LanguageKey.INVITED_YOU_GROUP,
                                    targetSession.AccountLanguage), clientSession.Character.Name),
                            YesPacket = new PjoinPacket
                            {
                                CharacterId = clientSession.Character.CharacterId,
                                RequestType = GroupRequestType.Accepted
                            },
                            NoPacket = new PjoinPacket
                            {
                                CharacterId = clientSession.Character.CharacterId,
                                RequestType = GroupRequestType.Declined
                            }
                        }).ConfigureAwait(false);
                    }

                    break;
                case GroupRequestType.Sharing:

                    if (clientSession.Character.Group!.Count == 1)
                    {
                        return;
                    }

                    await clientSession.SendPacket(new InfoPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_SHARE_INFO,
                            clientSession.Account.Language)
                    }).ConfigureAwait(false);

                    clientSession.Character.Group.Values
                        .Where(s => s.Item2.VisualId != clientSession.Character.CharacterId)
                        .ToList().ForEach(s =>
                        {
                            var session =
                                Broadcaster.Instance.GetCharacter(v =>
                                    v.VisualId == s.Item2.VisualId);

                            if (session == null)
                            {
                                return;
                            }

                            session.GroupRequestCharacterIds.TryAdd(s.Item2.VisualId, s.Item2.VisualId);
                            session.SendPacket(new DlgPacket
                            {
                                Question = GameLanguage.Instance.GetMessageFromKey(LanguageKey.INVITED_GROUP_SHARE,
                                    clientSession.Account.Language),
                                YesPacket = new PjoinPacket
                                {
                                    CharacterId = clientSession.Character.CharacterId,
                                    RequestType = GroupRequestType.AcceptedShare
                                },
                                NoPacket = new PjoinPacket
                                {
                                    CharacterId = clientSession.Character.CharacterId,
                                    RequestType = GroupRequestType.DeclinedShare
                                }
                            });
                        });

                    break;
                case GroupRequestType.Accepted:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);

                    if ((clientSession.Character.Group!.Count > 1) && (targetSession?.Group?.Count > 1))
                    {
                        return;
                    }

                    if (clientSession.Character.Group.IsGroupFull || (targetSession?.Group?.IsGroupFull ?? true))
                    {
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);

                        await targetSession!.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL,
                                targetSession.AccountLanguage)
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (clientSession.Character.Group.Count > 1)
                    {
                        targetSession.JoinGroup(clientSession.Character.Group);
                        await targetSession.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.JOINED_GROUP,
                                targetSession.AccountLanguage)
                        }).ConfigureAwait(false);
                    }
                    else if (targetSession.Group.Count > 1)
                    {
                        if (targetSession.Group.Type == GroupType.Group)
                        {
                            clientSession.Character.JoinGroup(targetSession.Group);
                        }
                    }
                    else
                    {
                        clientSession.Character.Group.GroupId = GroupAccess.Instance.GetNextGroupId();
                        targetSession.JoinGroup(clientSession.Character.Group);
                        await clientSession.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.JOINED_GROUP,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);

                        await targetSession.SendPacket(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_ADMIN,
                                targetSession.AccountLanguage)
                        }).ConfigureAwait(false);

                        targetSession.Group = clientSession.Character.Group;
                        clientSession.Character.GroupRequestCharacterIds.Clear();
                    }

                    if (clientSession.Character.Group.Type != GroupType.Group)
                    {
                        return;
                    }

                    var currentGroup = clientSession.Character.Group;

                    foreach (var member in currentGroup.Values.Where(s => s.Item2 is ICharacterEntity))
                    {
                        var session =
                            Broadcaster.Instance.GetCharacter(s =>
                                s.VisualId == member.Item2.VisualId);
                        session?.SendPacket(currentGroup.GeneratePinit());
                        session?.SendPackets(currentGroup.GeneratePst().Where(p => p.VisualId != session.VisualId));
                    }

                    GroupAccess.Instance.Groups[currentGroup.GroupId] = currentGroup;
                    await clientSession.Character.MapInstance.SendPacket(
                        clientSession.Character.Group.GeneratePidx(clientSession.Character)).ConfigureAwait(false);

                    break;
                case GroupRequestType.Declined:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);
                    await targetSession.SendPacket(new InfoPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_REFUSED,
                            targetSession.AccountLanguage)
                    }).ConfigureAwait(false);
                    break;
                case GroupRequestType.AcceptedShare:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    if (clientSession.Character.Group!.Count == 1)
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);
                    await clientSession.SendPacket(new MsgPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.ACCEPTED_SHARE,
                            clientSession.Account.Language),
                        Type = MessageType.White
                    }).ConfigureAwait(false);

                    //TODO: add a way to change respawn points when system will be done
                    break;
                case GroupRequestType.DeclinedShare:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);
                    await targetSession.SendPacket(new InfoPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHARED_REFUSED,
                            targetSession.AccountLanguage)
                    }).ConfigureAwait(false);
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.GROUPREQUESTTYPE_UNKNOWN));
                    break;
            }
        }
    }
}