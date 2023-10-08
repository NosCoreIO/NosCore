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

using NosCore.Data.Enumerations.Group;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Groups;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Services.IdService;
using NosCore.Networking;

namespace NosCore.PacketHandlers.Group
{
    public class PleavePacketHandler(IIdService<GameObject.Group> groupIdService) : PacketHandler<PleavePacket>,
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
                await clientSession.Character.LeaveGroupAsync().ConfigureAwait(false);

                if (isLeader)
                {
                    var targetsession = Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == group.Values.First().Item2.VisualId);

                    if (targetsession == null)
                    {
                        return;
                    }

                    await targetsession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.YouAreNowGroupLeader
                    }).ConfigureAwait(false);
                }

                if (group.Type != GroupType.Group)
                {
                    return;
                }

                foreach (var member in group.Values.Where(s => s.Item2 is ICharacterEntity))
                {
                    var character = member.Item2 as ICharacterEntity;
                    await (character == null ? Task.CompletedTask : character.SendPacketAsync(character.Group!.GeneratePinit())).ConfigureAwait(false);
                }

                await clientSession.SendPacketAsync(clientSession.Character.Group!.GeneratePinit()).ConfigureAwait(false);
                await clientSession.Character.MapInstance.SendPacketAsync(
                    clientSession.Character.Group.GeneratePidx(clientSession.Character)).ConfigureAwait(false);
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

                    await targetsession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.PartyDisbanded
                    }).ConfigureAwait(false);

                    await targetsession.LeaveGroupAsync().ConfigureAwait(false);
                    await targetsession.SendPacketAsync(targetsession.Group!.GeneratePinit()).ConfigureAwait(false);
                    await Broadcaster.Instance.SendPacketAsync(targetsession.Group.GeneratePidx(targetsession)).ConfigureAwait(false);
                }

                groupIdService.Items.TryRemove(group.GroupId, out _);
            }
        }
    }
}