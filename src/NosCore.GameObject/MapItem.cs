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
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject
{
    public class MapItem : ICountableEntity
    {
        private long _visualId;

        public long? OwnerId { get; set; }

        public long VisualId
        {
            get
            {
                if (_visualId == 0)
                {
                    _visualId = TransportFactory.Instance.GenerateTransportId();
                }

                return _visualId;
            }

            set => _visualId = value;
        }

        public short Amount { get; set; }

        public VisualType VisualType => VisualType.Object;

        public short VNum { get; set; }

        public byte Direction { get; set; }
        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public MapInstance MapInstance { get; set; }

        public DropPacket GenerateDrop()
        {
            return new DropPacket() { VNum = VNum, VisualId = VisualId, PositionX = PositionX, PositionY = PositionY, Amount = Amount, OwnerId = OwnerId };
        }
    }
}