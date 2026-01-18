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
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.ClientPackets.Groups;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Services.IdService;
using NosCore.Networking;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Group
{
    public class PleavePacketHandler(IIdService<GameObject.Group> groupIdService, ISessionRegistry sessionRegistry,
            IGroupPacketSystem groupPacketSystem)
        : PacketHandler<PleavePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PleavePacket bIPacket, ClientSession clientSession)
        {
            var group = clientSession.Player.Group;

            if (group!.Count == 1)
            {
                return;
            }

            if (group.Count > 2)
            {
                var isLeader = group.IsGroupLeader(clientSession.Player.CharacterId);
                clientSession.Player.Group?.LeaveGroup(clientSession.Player);
                var player = clientSession.Player;
                player.Group = null;
                clientSession.Player.GameState.InitializeGroup();

                if (isLeader)
                {
                    var firstMemberId = group.GetPlayerIds().FirstOrDefault();
                    var targetsession = sessionRegistry.GetPlayer(s => s.CharacterId == firstMemberId);

                    if (targetsession is not {} target)
                    {
                        return;
                    }

                    await (sessionRegistry.GetSenderByCharacterId(target.CharacterId)?.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.YouAreNowGroupLeader
                    }) ?? Task.CompletedTask).ConfigureAwait(false);
                }

                if (group.Type != GroupType.Group)
                {
                    return;
                }

                foreach (var characterId in group.GetPlayerIds())
                {
                    var memberPlayer = sessionRegistry.GetPlayer(s => s.CharacterId == characterId);
                    if (memberPlayer is {} member)
                    {
                        await (sessionRegistry.GetSenderByCharacterId(member.CharacterId)?.SendPacketAsync(member.Group!.GeneratePinit(sessionRegistry)) ?? Task.CompletedTask).ConfigureAwait(false);
                    }
                }

                await clientSession.SendPacketAsync(clientSession.Player.Group!.GeneratePinit(sessionRegistry)).ConfigureAwait(false);
                await clientSession.Player.MapInstance.SendPacketAsync(
                    groupPacketSystem.GeneratePidx(clientSession.Player.Group, clientSession.Player)).ConfigureAwait(false);
            }
            else
            {
                var memberList = new List<PlayerContext>();
                foreach (var characterId in group.GetPlayerIds())
                {
                    var player = sessionRegistry.GetPlayer(s => s.CharacterId == characterId);
                    if (player is {} p)
                    {
                        memberList.Add(p);
                    }
                }

                foreach (var targetsession in memberList)
                {
                    var sender = sessionRegistry.GetSenderByCharacterId(targetsession.CharacterId);
                    await (sender?.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.PartyDisbanded
                    }) ?? Task.CompletedTask).ConfigureAwait(false);

                    targetsession.Group?.LeaveGroup(targetsession);
                    targetsession.GameState.Group = null;
                    targetsession.GameState.InitializeGroup();
                    await (sender?.SendPacketAsync(targetsession.Group!.GeneratePinit(sessionRegistry)) ?? Task.CompletedTask).ConfigureAwait(false);
                    await targetsession.MapInstance.SendPacketAsync(groupPacketSystem.GeneratePidx(targetsession.Group!, targetsession)).ConfigureAwait(false);
                }

                groupIdService.Items.TryRemove(group.GroupId, out _);
            }
        }
    }
}