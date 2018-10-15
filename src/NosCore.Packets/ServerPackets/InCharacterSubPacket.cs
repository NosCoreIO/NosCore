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
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("in_character_subpacket")]
    public class InCharacterSubPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public AuthorityType Authority { get; set; }

        [PacketIndex(1)]
        public byte Gender { get; set; }

        [PacketIndex(2)]
        public byte HairStyle { get; set; }

        [PacketIndex(3)]
        public byte HairColor { get; set; }

        [PacketIndex(4)]
        public byte Class { get; set; }

        [PacketIndex(5)]
        public InEquipmentSubPacket Equipment { get; set; }

        [PacketIndex(6, RemoveSeparator = true)]
        public InAliveSubPacket InAliveSubPacket { get; set; }

        [PacketIndex(7)]
        public bool IsSitting { get; set; }

        [PacketIndex(8)]
        public long? GroupId { get; set; }

        [PacketIndex(9)]
        [UsedImplicitly]
        public byte Fairy { get; set; }

        [PacketIndex(10)]
        [UsedImplicitly]
        public byte FairyElement { get; set; }

        [PacketIndex(11)]
        [UsedImplicitly]
        public byte Unknown { get; set; } //TODO to find

        [PacketIndex(12)]
        [UsedImplicitly]
        public byte Morph { get; set; }

        [PacketIndex(13)]
        [UsedImplicitly]
        public byte WeaponUpgrade { get; set; }

        [PacketIndex(14)]
        [UsedImplicitly]
        public short WeaponRare { get; set; }

        [PacketIndex(15)]
        [UsedImplicitly]
        public byte ArmorUpgrade { get; set; }

        [PacketIndex(16)]
        [UsedImplicitly]
        public short ArmorRare { get; set; }

        [PacketIndex(17)]
        [UsedImplicitly]
        public long FamilyId { get; set; }

        [PacketIndex(18)]
        [UsedImplicitly]
        public string FamilyName { get; set; }

        [PacketIndex(19)]
        [UsedImplicitly]
        public short ReputIco { get; set; }

        [PacketIndex(20)]
        [UsedImplicitly]
        public bool Invisible { get; set; }

        [PacketIndex(21)]
        [UsedImplicitly]
        public byte MorphUpgrade { get; set; }

        [PacketIndex(22)]
        public byte Faction { get; set; }

        [PacketIndex(23)]
        [UsedImplicitly]
        public byte MorphUpgrade2 { get; set; }

        [PacketIndex(24)]
        [UsedImplicitly]
        public byte Level { get; set; }

        [PacketIndex(25)]
        [UsedImplicitly]
        public byte FamilyLevel { get; set; }

        [PacketIndex(26)]
        [UsedImplicitly]
        public bool ArenaWinner { get; set; }

        [PacketIndex(27)]
        [UsedImplicitly]
        public short Compliment { get; set; }

        [PacketIndex(28)]
        [UsedImplicitly]
        public byte Size { get; set; }

        [PacketIndex(29)]
        [UsedImplicitly]
        public byte HeroLevel { get; set; }

        #endregion
    }
}