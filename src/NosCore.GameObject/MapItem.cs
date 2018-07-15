using System;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.GameObject
{
    public class MapItem : ICountableEntity
    {
        public short Amount { get; set; }

        public VisualType VisualType => VisualType.Object;

        public short VNum { get; set; }

        public long VisualId { get; set; }

        public byte Direction { get; set; }
        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
    }
}
