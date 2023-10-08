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

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NodaTime;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class MapItem(long visualId) : ICountableEntity, IRequestableEntity<Tuple<MapItem, GetPacket>>
    {
        public IItemInstance? ItemInstance { get; set; }

        public long? OwnerId { get; set; }
        public Instant DroppedAt { get; set; }

        public long VisualId
        {
            get => visualId;

            set => visualId = value;
        }

        public short Amount => ItemInstance?.Amount ?? 0;

        public VisualType VisualType => VisualType.Object;

        public short VNum => ItemInstance?.ItemVNum ?? 0;

        public byte Direction { get; set; }
        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public MapInstance MapInstance { get; set; } = null!;

        public List<Task> HandlerTasks { get; set; } = new();
        public Dictionary<Type, Subject<RequestData<Tuple<MapItem, GetPacket>>>> Requests { get; set; } = new()
        {
            [typeof(IGetMapItemEventHandler)] = new()
        };

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