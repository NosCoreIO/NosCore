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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.UI;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.Infastructure;
using NosCore.Networking;
using NosCore.Shared.I18N;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;

namespace NosCore.PacketHandlers.Group
{
    public class PjoinPacketHandler(ILogger logger, IBlacklistHub blacklistHttpCLient, IClock clock,
            IIdService<GameObject.Services.GroupService.Group> groupIdService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IGameLanguageLocalizer gameLanguageLocalizer,
            ISessionRegistry sessionRegistry)
        : PacketHandler<PjoinPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PjoinPacket pjoinPacket, ClientSession clientSession)
        {
            var targetSession =
                sessionRegistry.GetCharacter(s =>
                    s.VisualId == pjoinPacket.CharacterId);

            if ((targetSession == null) && (pjoinPacket.RequestType != GroupRequestType.Sharing))
            {
                logger.Error(gameLanguageLocalizer[LanguageKey.UNABLE_TO_REQUEST_GROUP,
                    clientSession.Account.Language]);
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
                        });
                        return;
                    }

                    if ((targetSession.Group!.Count > 1) && (clientSession.Character.Group!.Count > 1))
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.AlreadyInAnotherGroup
                        });
                        return;
                    }

                    var blacklisteds = await blacklistHttpCLient.GetBlacklistedAsync(clientSession.Character.VisualId);
                    if (blacklisteds != null && blacklisteds.Any(s => s.CharacterId == pjoinPacket.CharacterId))
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.AlreadyBlacklisted
                        });
                        return;
                    }

                    if (targetSession.GroupRequestBlocked)
                    {
                        await clientSession.SendPacketAsync(new MsgiPacket
                        {
                            Type = MessageType.Default,
                            Message = Game18NConstString.GroupBlocked
                        });
                        return;
                    }

                    if (clientSession.Character.LastGroupRequest != null)
                    {
                        var diffTimeSpan = ((Instant)clientSession.Character.LastGroupRequest).Plus(Duration.FromSeconds(5)) - clock.GetCurrentInstant();
                        if (diffTimeSpan.Seconds > 0 && diffTimeSpan.Seconds <= 5)
                        {
                            await clientSession.SendPacketAsync(new InfoiPacket
                            {
                                Message = Game18NConstString.CannotSendInvite,
                                ArgumentType = 4,
                                Game18NArguments = { diffTimeSpan.Seconds }
                            });
                            return;
                        }
                    }

                    clientSession.Character.GroupRequestCharacterIds.TryAdd(pjoinPacket.CharacterId, pjoinPacket.CharacterId);
                    clientSession.Character.LastGroupRequest = clock.GetCurrentInstant();

                    if (((clientSession.Character.Group!.Count == 1) ||
                            (clientSession.Character.Group.Type == GroupType.Group))
                        && ((targetSession.Group.Count == 1) || (targetSession.Group.Type == GroupType.Group)))
                    {
                        await clientSession.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.YouInvitedToGroup,
                            ArgumentType = 1,
                            Game18NArguments = { targetSession.Name ?? "" }
                        });
                        await targetSession.SendPacketAsync(new Dlgi2Packet
                        {
                            Question = Game18NConstString.GroupInvite,
                            ArgumentType = 1,
                            Game18NArguments = { clientSession.Character.Name },
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
                        });
                    }

                    break;
                case GroupRequestType.Sharing:

                    if (clientSession.Character.Group!.Count == 1)
                    {
                        return;
                    }

                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.CanNotChangeGroupMode
                    });

                    await Task.WhenAll(clientSession.Character.Group.Values
                        .Where(s => s.Item2.VisualId != clientSession.Character.CharacterId)
                        .Select(s =>
                        {
                            var session =
                                sessionRegistry.GetCharacter(v =>
                                    v.VisualId == s.Item2.VisualId);

                            if (session == null)
                            {
                                return Task.CompletedTask;
                            }

                            session.GroupRequestCharacterIds.TryAdd(s.Item2.VisualId, s.Item2.VisualId);
                            return session.SendPacketAsync(new Dlgi2Packet
                            {
                                Question = Game18NConstString.ConfirmSetPointOfReturn,
                                ArgumentType = 1,
                                Game18NArguments = { s.Item2.Name ?? "" },
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
                        }));

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
                        });

                        await targetSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.GroupIsFull
                        });
                        return;
                    }

                    if (clientSession.Character.Group.Count > 1)
                    {
                        targetSession.JoinGroup(clientSession.Character.Group);
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
                        clientSession.Character.Group.GroupId = groupIdService.GetNextId();
                        targetSession.JoinGroup(clientSession.Character.Group);

                        await targetSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.YouAreNowGroupLeader
                        });

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
                            sessionRegistry.GetCharacter(s =>
                                s.VisualId == member.Item2.VisualId);
                        session?.SendPacketAsync(currentGroup.GeneratePinit());
                        session?.SendPacketsAsync(currentGroup.GeneratePst().Where(p => p.VisualId != session.VisualId));
                    }

                    groupIdService.Items[currentGroup.GroupId] = currentGroup;
                    await clientSession.Character.MapInstance.SendPacketAsync(
                        clientSession.Character.Group.GeneratePidx(clientSession.Character));

                    break;
                case GroupRequestType.Declined:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);
                    await clientSession.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.GroupInviteRejected,
                        ArgumentType = 1,
                        Game18NArguments = { targetSession.Name ?? "" }
                    });
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
                        Type = MessageType.Default,
                        Message = Game18NConstString.ChangedSamePointOfReturn
                    });
                    await clientSession.SendPacketAsync(new Msgi2Packet
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SomeoneChangedPointOfReturn,
                        ArgumentType = 1,
                        Game18NArguments = { targetSession.Name ?? "" }
                    });
                    //TODO: add a way to change respawn points when system will be done
                    break;
                case GroupRequestType.DeclinedShare:
                    if (targetSession == null || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);
                    await targetSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.RefusedSharePointOfReturn
                    });
                    await clientSession.SendPacketAsync(new Msgi2Packet
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SomeoneRefusedToSharePointOfReturn,
                        ArgumentType = 1,
                        Game18NArguments = { targetSession.Name ?? "" }
                    });
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.GROUPREQUESTTYPE_UNKNOWN]);
                    break;
            }
        }
    }
}