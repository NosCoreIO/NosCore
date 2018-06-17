using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.AliveEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject
{
    public class MapItem : ICountableEntity
    {
        public short Amount { get; set; }

        public byte VisualType => 9;

        public short VNum { get; set; }

        public long VisualId { get; set; }

        public byte Direction { get; set; }
        public Guid MapInstanceId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
    }
}
