using System;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject
{
    public class MapItem : ICountableEntity
    {
        private long _visualId;
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

        public long? OwnerId { get; set; }

        public DropPacket GenerateDrop()
        {
            return new DropPacket() { VNum = VNum, VisualId = VisualId, PositionX = PositionX, PositionY = PositionY, Amount = Amount, OwnerId = OwnerId };
        }
    }
}