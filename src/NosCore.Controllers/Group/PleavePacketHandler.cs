using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Groups;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using Serilog;

namespace NosCore.PacketHandlers.Group
{
    public class PleavePacketHandler : PacketHandler<PleavePacket>, IWorldPacketHandler
    {
        public override void Execute(PleavePacket bIPacket, ClientSession clientSession)
        {

            var group = clientSession.Character.Group;

            if (group.Count == 1)
            {
                return;
            }

            if (group.Count > 2)
            {
                var isLeader = group.IsGroupLeader(clientSession.Character.CharacterId);
                clientSession.Character.LeaveGroup();

                if (isLeader)
                {
                    var targetsession = Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == group.Values.First().Item2.VisualId);

                    if (targetsession == null)
                    {
                        return;
                    }

                    targetsession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NEW_LEADER, clientSession.Account.Language)
                    });
                }

                if (group.Type != GroupType.Group)
                {
                    return;
                }

                foreach (var member in group.Values.Where(s => s.Item2 is ICharacterEntity))
                {
                    var character = member.Item2 as ICharacterEntity;
                    character.SendPacket(character.Group.GeneratePinit());
                    character.SendPacket(new MsgPacket
                    {
                        Message = string.Format(
                            Language.Instance.GetMessageFromKey(LanguageKey.LEAVE_GROUP, clientSession.Account.Language),
                            clientSession.Character.Name)
                    });
                }

                clientSession.SendPacket(clientSession.Character.Group.GeneratePinit());
                clientSession.SendPacket(new MsgPacket
                { Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_LEFT, clientSession.Account.Language) });
                clientSession.Character.MapInstance.Sessions.SendPacket(
                    clientSession.Character.Group.GeneratePidx(clientSession.Character));
            }
            else
            {
                var memberList = new List<INamedEntity>();
                memberList.AddRange(group.Values.Select(s => s.Item2));

                foreach (var member in memberList.Where(s => s is ICharacterEntity))
                {
                    var targetsession =
                        Broadcaster.Instance.GetCharacter(s =>
                            s.VisualId == member.VisualId);

                    if (targetsession == null)
                    {
                        continue;
                    }

                    targetsession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_CLOSED, targetsession.AccountLanguage),
                        Type = MessageType.White
                    });

                    targetsession.LeaveGroup();
                    targetsession.SendPacket(targetsession.Group.GeneratePinit());
                    Broadcaster.Instance.Sessions.SendPacket(targetsession.Group.GeneratePidx(targetsession));
                }

                GroupAccess.Instance.Groups.TryRemove(group.GroupId, out _);
            }
        }
    }
}
