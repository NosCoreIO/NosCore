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

using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.GameObject.Networking
{
    public class BroadcastPacket
    {
        public BroadcastPacket(ClientSession session, PacketDefinition packet, ReceiverType receiver,
            string someonesCharacterName = "", long someonesCharacterId = -1, int xCoordinate = 0, int yCoordinate = 0)
        {
            Sender = session;
            Packet = packet;
            Receiver = receiver;
            SomeonesCharacterName = someonesCharacterName;
            SomeonesCharacterId = someonesCharacterId;
            XCoordinate = xCoordinate;
            YCoordinate = yCoordinate;
        }

        public PacketDefinition Packet { get; set; }

        public ReceiverType Receiver { get; set; }

        public ClientSession Sender { get; set; }

        public long SomeonesCharacterId { get; set; }

        public string SomeonesCharacterName { get; set; }

        public int XCoordinate { get; set; }

        public int YCoordinate { get; set; }
    }
}