﻿//  __  _  __    __   ___ __  ___ ___
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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.UI;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.Networking;
using NosCore.Shared.I18N;

namespace NosCore.PacketHandlers.Group
{
    public class PjoinPacketHandler : PacketHandler<PjoinPacket>, IWorldPacketHandler
    {
        private readonly IBlacklistHttpClient _blacklistHttpCLient;
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private readonly IIdService<GameObject.Group> _groupIdService;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public PjoinPacketHandler(ILogger logger, IBlacklistHttpClient blacklistHttpCLient, IClock clock, IIdService<GameObject.Group> groupIdService, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _groupIdService = groupIdService;
            _logger = logger;
            _blacklistHttpCLient = blacklistHttpCLient;
            _clock = clock;
            _logLanguage = logLanguage;
        }

        public override async Task ExecuteAsync(PjoinPacket pjoinPacket, ClientSession clientSession)
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
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.GroupIsFull
                        }).ConfigureAwait(false);
                        return;
                    }

                    if ((targetSession.Group!.Count > 1) && (clientSession.Character.Group!.Count > 1))
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.AlreadyInAnotherGroup
                        }).ConfigureAwait(false);
                        return;
                    }

                    var blacklisteds = await _blacklistHttpCLient.GetBlackListsAsync(clientSession.Character.VisualId).ConfigureAwait(false);
                    if (blacklisteds != null && blacklisteds.Any(s => s.CharacterId == pjoinPacket.CharacterId))
                    {
                        await clientSession.SendPacketAsync(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (targetSession.GroupRequestBlocked)
                    {
                        await clientSession.SendPacketAsync(new MsgiPacket
                        {
                            Message = Game18NConstString.GroupBlocked
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (clientSession.Character.LastGroupRequest != null)
                    {
                        var diffTimeSpan = ((Instant)clientSession.Character.LastGroupRequest).Plus(Duration.FromSeconds(5)) - _clock.GetCurrentInstant();
                        if (diffTimeSpan.Seconds > 0 && diffTimeSpan.Seconds <= 5)
                        {
                            await clientSession.SendPacketAsync(new InfoiPacket
                            {
                                Message = Game18NConstString.CannotSendInvite,
                                ArgumentType = 4,
                                Game18NArguments = new object[] { diffTimeSpan.Seconds }
                            }).ConfigureAwait(false);
                            return;
                        }
                    }

                    clientSession.Character.GroupRequestCharacterIds.TryAdd(pjoinPacket.CharacterId, pjoinPacket.CharacterId);
                    clientSession.Character.LastGroupRequest = _clock.GetCurrentInstant();

                    if (((clientSession.Character.Group!.Count == 1) ||
                            (clientSession.Character.Group.Type == GroupType.Group))
                        && ((targetSession.Group.Count == 1) || (targetSession.Group.Type == GroupType.Group)))
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.GroupInvite
                        }).ConfigureAwait(false);
                        await targetSession.SendPacketAsync(new DlgPacket
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

                    await clientSession.SendPacketAsync(new InfoPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.GROUP_SHARE_INFO,
                            clientSession.Account.Language)
                    }).ConfigureAwait(false);

                    await Task.WhenAll(clientSession.Character.Group.Values
                        .Where(s => s.Item2.VisualId != clientSession.Character.CharacterId)
                        .Select(s =>
                        {
                            var session =
                                Broadcaster.Instance.GetCharacter(v =>
                                    v.VisualId == s.Item2.VisualId);

                            if (session == null)
                            {
                                return Task.CompletedTask;
                            }

                            session.GroupRequestCharacterIds.TryAdd(s.Item2.VisualId, s.Item2.VisualId);
                            return session.SendPacketAsync(new DlgPacket
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
                        })).ConfigureAwait(false);

                    break;
                case GroupRequestType.Accepted:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);

                    if ((clientSession.Character.Group!.Count > 1) && (targetSession.Group?.Count > 1))
                    {
                        return;
                    }

                    if (clientSession.Character.Group.IsGroupFull || (targetSession.Group?.IsGroupFull ?? true))
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.GroupIsFull
                        }).ConfigureAwait(false);

                        await targetSession!.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.GroupIsFull
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (clientSession.Character.Group.Count > 1)
                    {
                        targetSession.JoinGroup(clientSession.Character.Group);
                        await targetSession.SendPacketAsync(new InfoPacket
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
                        clientSession.Character.Group.GroupId = _groupIdService.GetNextId();
                        targetSession.JoinGroup(clientSession.Character.Group);
                        await clientSession.SendPacketAsync(new InfoPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.JOINED_GROUP,
                                clientSession.Account.Language)
                        }).ConfigureAwait(false);

                        await targetSession.SendPacketAsync(new InfoPacket
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
                        session?.SendPacketAsync(currentGroup.GeneratePinit());
                        session?.SendPacketsAsync(currentGroup.GeneratePst().Where(p => p.VisualId != session.VisualId));
                    }

                    _groupIdService.Items[currentGroup.GroupId] = currentGroup;
                    await clientSession.Character.MapInstance.SendPacketAsync(
                        clientSession.Character.Group.GeneratePidx(clientSession.Character)).ConfigureAwait(false);

                    break;
                case GroupRequestType.Declined:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);
                    await targetSession.SendPacketAsync(new InfoPacket
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
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Message = Game18NConstString.ChangedSamePointOfReturn
                    }).ConfigureAwait(false);

                    //TODO: add a way to change respawn points when system will be done
                    break;
                case GroupRequestType.DeclinedShare:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);
                    await targetSession.SendPacketAsync(new InfoPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHARED_REFUSED,
                            targetSession.AccountLanguage)
                    }).ConfigureAwait(false);
                    break;
                default:
                    _logger.Error(_logLanguage[LogLanguageKey.GROUPREQUESTTYPE_UNKNOWN]);
                    break;
            }
        }
    }
}