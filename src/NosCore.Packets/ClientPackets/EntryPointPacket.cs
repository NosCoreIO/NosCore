//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using JetBrains.Annotations;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("EntryPoint", 3, AnonymousAccess = true)]
    public class EntryPointPacket : PacketDefinition
    {
        [PacketIndex(0)]
        [UsedImplicitly]
        public string Title { get; set; }

        [PacketIndex(1)]
        [UsedImplicitly]
        public string Packet1Id { get; set; }

        [PacketIndex(2)]
        public string Name { get; set; }

        [PacketIndex(3)]
        [UsedImplicitly]
        public string Packet2Id { get; set; }

        [PacketIndex(4)]
        public string Password { get; set; }
    }
}