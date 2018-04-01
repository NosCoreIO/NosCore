using System;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IVisualEntity
    {
        byte VisualType { get; set; }

        short VNum { get; set; }

        long VisualId { get; set; }

        byte? Direction { get; set; }

        Guid MapInstanceId { get; set; }

        short PositionX { get; set; }

        short PositionY { get; set; }
   }
}
