//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Core.I18N;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Group
{
    public class PjoinPacketHandler(ILogger<PjoinPacketHandler> logger, IBlacklistHub blacklistHttpCLient, IClock clock,
            IIdService<GameObject.Services.GroupService.Group> groupIdService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IGameLanguageLocalizer gameLanguageLocalizer,
            ISessionRegistry sessionRegistry)
        : PacketHandler<PjoinPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PjoinPacket pjoinPacket, ClientSession clientSession)
        {
            var hasTargetSession = sessionRegistry.TryGetCharacter(s =>
                s.VisualId == pjoinPacket.CharacterId, out var targetSession);

            if (!hasTargetSession && (pjoinPacket.RequestType != GroupRequestType.Sharing))
            {
                logger.LogError(gameLanguageLocalizer[LanguageKey.UNABLE_TO_REQUEST_GROUP,
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

                    if (!hasTargetSession || targetSession.Group.IsGroupFull)
                    {
                        await clientSession.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.GroupIsFull
                        });
                        return;
                    }

                    if ((targetSession.Group.Count > 1) && (clientSession.Character.Group.Count > 1))
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

                    if (((clientSession.Character.Group.Count == 1) ||
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

                    if (clientSession.Character.Group.Count == 1)
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
                            if (!sessionRegistry.TryGetCharacter(v =>
                                v.VisualId == s.Item2.VisualId, out var session))
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
                    if (!hasTargetSession || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(clientSession.Character.CharacterId, out _);

                    if ((clientSession.Character.Group.Count > 1) && (targetSession.Group.Count > 1))
                    {
                        return;
                    }

                    if (clientSession.Character.Group.IsGroupFull || targetSession.Group.IsGroupFull)
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
                        if (sessionRegistry.TryGetCharacter(s =>
                            s.VisualId == member.Item2.VisualId, out var session))
                        {
                            await session.SendPacketAsync(currentGroup.GeneratePinit());
                            await session.SendPacketsAsync(currentGroup.GeneratePst().Where(p => p.VisualId != session.VisualId));
                        }
                    }

                    groupIdService.Items[currentGroup.GroupId] = currentGroup;
                    await clientSession.Character.MapInstance.SendPacketAsync(
                        clientSession.Character.Group.GeneratePidx(clientSession.Character));

                    break;
                case GroupRequestType.Declined:
                    if (!hasTargetSession || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
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
                    if (!hasTargetSession || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
                    {
                        return;
                    }

                    if (clientSession.Character.Group.Count == 1)
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
                    if (!hasTargetSession || !targetSession.GroupRequestCharacterIds.Values.Contains(clientSession.Character.CharacterId))
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
                    logger.LogError(logLanguage[LogLanguageKey.GROUPREQUESTTYPE_UNKNOWN]);
                    break;
            }
        }
    }
}
