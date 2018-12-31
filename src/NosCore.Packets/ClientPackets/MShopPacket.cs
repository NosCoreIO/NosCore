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

using NosCore.Core;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Security.Permissions;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("m_shop")]
    public class MShopPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public CreateShopPacketType Type { get; set; }

        [PacketIndex(1, IsOptional = true, SpecialSeparator = " ")]
        public List<MShopItemSubPacket> ItemList { get; set; }

        [PacketIndex(2, SerializeToEnd = true, IsOptional = true)]
        public string Name { get; set; }
    }
}