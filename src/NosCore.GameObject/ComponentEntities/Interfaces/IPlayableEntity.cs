using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface IPlayableEntity : INamedEntity
    {
        DateTime LastGroupJoin { get; set; }
    }
}
