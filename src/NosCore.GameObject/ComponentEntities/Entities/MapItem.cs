//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.ComponentEntities.Entities
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
