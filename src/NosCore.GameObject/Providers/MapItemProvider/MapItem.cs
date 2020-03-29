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
using System.Reactive.Subjects;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;

namespace NosCore.GameObject.Providers.MapItemProvider
{
    public class MapItem : ICountableEntity, IRequestableEntity<Tuple<MapItem, GetPacket>>
    {
        private long _visualId;

        public MapItem()
        {
            Requests = new Subject<RequestData<Tuple<MapItem, GetPacket>>>();
        }

        public IItemInstance? ItemInstance { get; set; }

        public long? OwnerId { get; set; }
        public DateTime DroppedAt { get; set; }

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

        public short Amount => ItemInstance?.Amount ?? 0;

        public VisualType VisualType => VisualType.Object;

        public short VNum => ItemInstance?.ItemVNum ?? 0;

        public byte Direction { get; set; }
        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public MapInstance MapInstance { get; set; } = null!;

        public Subject<RequestData<Tuple<MapItem, GetPacket>>>? Requests { get; set; }

        public DropPacket GenerateDrop()
        {
            return new DropPacket
            {
                VNum = VNum,
                VisualId = VisualId,
                PositionX = PositionX,
                PositionY = PositionY,
                Amount = Amount,
                OwnerId = OwnerId
            };
        }
    }
}