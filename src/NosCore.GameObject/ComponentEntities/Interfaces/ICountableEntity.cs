using NosCore.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject
{
    public interface ICountableEntity : IVisualEntity
    {
        short Amount { get; set; }
    }
}
