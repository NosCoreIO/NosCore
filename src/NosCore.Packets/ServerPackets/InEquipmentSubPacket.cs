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

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("in_equipment_subpacket")]
    public class InEquipmentSubPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public short? Hat { get; set; }

        [PacketIndex(1)]
        public short? Armor { get; set; }

        [PacketIndex(2)]
        public short? MainWeapon { get; set; }

        [PacketIndex(3)]
        public short? SecondaryWeapon { get; set; }

        [PacketIndex(4)]
        public short? Mask { get; set; }

        [PacketIndex(5)]
        public short? Fairy { get; set; }

        [PacketIndex(6)]
        public short? CostumeSuit { get; set; }

        [PacketIndex(7)]
        public short? CostumeHat { get; set; }

        [PacketIndex(8)]
        public short? WeaponSkin { get; set; }
    }
}