using NosCore.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject
{
    public interface INamedEntity : IAliveEntity
    {
        string Name { get; set; }
   }
}
