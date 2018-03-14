using NosCore.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject
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
