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


using NosCore.Packets.ServerPackets.Shop;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs.Systems;

public interface IGroupPacketSystem
{
    PidxPacket GeneratePidx(Group group, PlayerContext player);
}

public class GroupPacketSystem : IGroupPacketSystem
{
    public PidxPacket GeneratePidx(Group group, PlayerContext player)
    {
        return new PidxPacket
        {
            GroupId = group.Count == 1 ? -1 : group.GroupId,
            SubPackets = group.Count == 1
                ? new List<PidxSubPacket?> { new PidxSubPacket { IsGrouped = true, VisualId = player.VisualId } }
                : group.GetMemberIds().Select(m => new PidxSubPacket { IsGrouped = false, VisualId = m.VisualId }).ToList() as List<PidxSubPacket?>
        };
    }
}
