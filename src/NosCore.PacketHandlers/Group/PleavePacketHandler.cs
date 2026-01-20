//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Groups;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Group
{
    public class PleavePacketHandler(IIdService<GameObject.Services.GroupService.Group> groupIdService, ISessionRegistry sessionRegistry, ISessionGroupFactory sessionGroupFactory) : PacketHandler<PleavePacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PleavePacket bIPacket, ClientSession clientSession)
        {
            var group = clientSession.Character.Group;

            if (group!.Count == 1)
            {
                return;
            }

            if (group.Count > 2)
            {
                var isLeader = group.IsGroupLeader(clientSession.Character.CharacterId);
                await clientSession.Character.LeaveGroupAsync(sessionGroupFactory, sessionRegistry);

                if (isLeader)
                {
                    var targetsession = sessionRegistry.GetCharacter(s =>
                        s.VisualId == group.Values.First().Item2.VisualId);

                    if (targetsession == null)
                    {
                        return;
                    }

                    await targetsession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.YouAreNowGroupLeader
                    });
                }

                if (group.Type != GroupType.Group)
                {
                    return;
                }

                foreach (var member in group.Values.Where(s => s.Item2 is ICharacterEntity))
                {
                    var character = member.Item2 as ICharacterEntity;
                    await (character == null ? Task.CompletedTask : character.SendPacketAsync(character.Group!.GeneratePinit()));
                }

                await clientSession.SendPacketAsync(clientSession.Character.Group!.GeneratePinit());
                await clientSession.Character.MapInstance.SendPacketAsync(
                    clientSession.Character.Group.GeneratePidx(clientSession.Character));
            }
            else
            {
                var memberList = new List<INamedEntity>();
                memberList.AddRange(group.Values.Select(s => s.Item2));

                foreach (var member in memberList.Where(s => s is ICharacterEntity))
                {
                    var targetsession =
                        sessionRegistry.GetCharacter(s =>
                            s.VisualId == member.VisualId);

                    if (targetsession == null)
                    {
                        continue;
                    }

                    await targetsession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.PartyDisbanded
                    });

                    await targetsession.LeaveGroupAsync(sessionGroupFactory, sessionRegistry);
                    await targetsession.SendPacketAsync(targetsession.Group!.GeneratePinit());
                    await targetsession.MapInstance.SendPacketAsync(targetsession.Group.GeneratePidx(targetsession));
                }

                groupIdService.Items.TryRemove(group.GroupId, out _);
            }
        }
    }
}
