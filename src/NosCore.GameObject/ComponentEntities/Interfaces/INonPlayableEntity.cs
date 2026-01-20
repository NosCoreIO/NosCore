//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.StaticEntities;
using System;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface INonPlayableEntity : IAliveEntity
    {
        bool IsMoving { get; }

        short Effect { get; }

        short EffectDelay { get; }

        bool IsDisabled { get; }

        NpcMonsterDto NpcMonster { get; set; }

        Instant LastMove { get; set; }

        IDisposable? Life { get; set; }

        new bool IsAlive { get; set; }

        new byte Speed { get; set; }
    }
}
