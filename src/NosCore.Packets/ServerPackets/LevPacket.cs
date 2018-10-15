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
using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("lev")]
    public class LevPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Level { get; set; }

        [PacketIndex(1)]
        public long LevelXp { get; set; }

        [PacketIndex(2)]
        public byte JobLevel { get; set; }

        [PacketIndex(3)]
        public long JobLevelXp { get; set; }

        [PacketIndex(4)]
        public long XpLoad { get; set; }

        [PacketIndex(5)]
        public long JobXpLoad { get; set; }

        [PacketIndex(6)]
        public long Reputation { get; set; }

        [PacketIndex(7)]
        public int SkillCp { get; set; }

        [PacketIndex(8)]
        public long HeroXp { get; set; }

        [PacketIndex(9)]
        public byte HeroLevel { get; set; }

        [PacketIndex(10)]
        public long HeroXpLoad { get; set; }
    }
}
