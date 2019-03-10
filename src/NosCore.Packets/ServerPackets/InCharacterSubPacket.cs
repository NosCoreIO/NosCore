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
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Character;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("in_character_subpacket")]
    public class InCharacterSubPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public AuthorityType Authority { get; set; }

        [PacketIndex(1)]
        public GenderType Gender { get; set; }

        [PacketIndex(2)]
        public byte HairStyle { get; set; }

        [PacketIndex(3)]
        public byte HairColor { get; set; }

        [PacketIndex(4)]
        public CharacterClassType Class { get; set; }

        [PacketIndex(5)]
        public InEquipmentSubPacket Equipment { get; set; }

        [PacketIndex(6, SpecialSeparator = " ")]
        public InAliveSubPacket InAliveSubPacket { get; set; }

        [PacketIndex(7)]
        public bool IsSitting { get; set; }

        [PacketIndex(8)]
        public long? GroupId { get; set; }

        [PacketIndex(9)]
        public byte Fairy { get; set; }

        [PacketIndex(10)]
        public byte FairyElement { get; set; }

        [PacketIndex(11)]
        public byte Unknown { get; set; } //TODO to find

        [PacketIndex(12)]
        public byte Morph { get; set; }

        //TODO: Find what Unknown2 & 3 are made for
        [PacketIndex(13)]
        public byte Unknown2 { get; set; }

        [PacketIndex(14)]
        public byte Unknown3 { get; set; }

        [PacketIndex(15, SpecialSeparator = "")]
        public UpgradeRareSubPacket WeaponUpgradeRareSubPacket { get; set; }

        [PacketIndex(16, SpecialSeparator = "")]
        public UpgradeRareSubPacket ArmorUpgradeRareSubPacket { get; set; }

        [PacketIndex(17)]
        public long FamilyId { get; set; }

        [PacketIndex(18)]
        public string FamilyName { get; set; }

        [PacketIndex(19)]
        public short ReputIco { get; set; }

        [PacketIndex(20)]
        public bool Invisible { get; set; }

        [PacketIndex(21)]
        public byte MorphUpgrade { get; set; }

        [PacketIndex(22)]
        public byte Faction { get; set; }

        [PacketIndex(23)]
        public byte MorphUpgrade2 { get; set; }

        [PacketIndex(24)]
        public byte Level { get; set; }

        [PacketIndex(25)]
        public byte FamilyLevel { get; set; }

        [PacketIndex(26)]
        public bool ArenaWinner { get; set; }

        [PacketIndex(27)]
        public short Compliment { get; set; }

        [PacketIndex(28)]
        public byte Size { get; set; }

        [PacketIndex(29)]
        public byte HeroLevel { get; set; }
    }
}