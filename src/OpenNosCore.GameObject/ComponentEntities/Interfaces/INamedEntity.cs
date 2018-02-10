using OpenNosCore.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNosCore.GameObject
{
    public interface INamedEntity : IAliveEntity
    {
        string Name { get; set; }
   }
}
