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

using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("eq")]
    public class EqPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long VisualId { get; set; }

        [PacketIndex(1)]
        public byte Visibility { get; set; }

        [PacketIndex(2)]
        public GenderType Gender { get; set; }

        [PacketIndex(3)]
        public HairStyleType HairStyle { get; set; }

        [PacketIndex(4)]
        public HairColorType Haircolor { get; set; }

        [PacketIndex(5)]
        public CharacterClassType ClassType { get; set; }

        [PacketIndex(6)]
        public InEquipmentSubPacket EqSubPacket { get; set; }

        [PacketIndex(7, SpecialSeparator = "")]
        public UpgradeRareSubPacket WeaponUpgradeRarePacket { get; set; }

        [PacketIndex(8, SpecialSeparator = "")]
        public UpgradeRareSubPacket ArmorUpgradeRarePacket { get; set; }
    }
}