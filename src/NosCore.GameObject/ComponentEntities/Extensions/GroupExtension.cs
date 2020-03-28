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

using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class GroupExtension
    {
        public static PidxPacket GeneratePidx(this Group group, INamedEntity entity)
        {
            return new PidxPacket
            {
                GroupId = group.Count == 1 ? -1 : group.GroupId,
                SubPackets = group.Count == 1 ? new List<PidxSubPacket> {entity.GenerateSubPidx(true)}
                    : group.Values.Select(s => s.Item2.GenerateSubPidx()).ToList()
            };
        }
    }
}