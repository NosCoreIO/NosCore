using System;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IVisualEntity
    {
        VisualType VisualType { get; }

        short VNum { get; set; }

        long VisualId { get; }

        byte Direction { get; set; }

        Guid MapInstanceId { get; set; }

        MapInstance MapInstance { get; set; }

        short PositionX { get; set; }

        short PositionY { get; set; }
    }
}