using System;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface INonPlayableEntity : INamedEntity
    {
        bool IsMoving { get; set; }

        short Effect { get; set; }

        short EffectDelay { get; set; }

        bool IsDisabled { get; set; }

        DateTime LastMove { get; set; }
        NpcMonsterDTO NpcMonster { get; set; }

    }
}