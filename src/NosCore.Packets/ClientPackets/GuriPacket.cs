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
    [PacketHeader("guri")]
    public class GuriPacket : PacketDefinition
    {
        #region Properties        

        [PacketIndex(0)]
        public int Type { get; set; }

        [PacketIndex(1)]
        [UsedImplicitly]
        public int Argument { get; set; }

        [PacketIndex(2)]
        public long? VisualEntityId { get; set; }

        [PacketIndex(3)]
        public int Data { get; set; }

        [PacketIndex(4, true, false, false, SerializeToEnd = true)]
        public string Value { get; set; }

        #endregion
    }
}