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

using System;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Core.Serializing
{
    [AttributeUsage(AttributeTargets.All)]
    public class PacketHeaderAttribute : Attribute
    {
        public PacketHeaderAttribute(string identification)
        {
            Identification = identification;
        }

        public PacketHeaderAttribute(string identification, byte amount)
        {
            Identification = identification;
            Amount = amount;
        }

        /// <summary>
        ///     Permission to handle the packet
        /// </summary>
        public AuthorityType Authority { get; set; }

        /// <summary>
        ///     Unique identification of the Packet
        /// </summary>
        public string Identification { get; set; }

        /// <summary>
        ///     Amount of tcp message to create the Packet
        /// </summary>
        public byte Amount { get; set; }

        public bool AnonymousAccess { get; set; }
    }
}