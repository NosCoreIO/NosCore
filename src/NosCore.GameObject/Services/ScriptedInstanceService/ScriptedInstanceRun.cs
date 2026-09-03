//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class ScriptedInstanceRun(ScriptedInstance entrance, IReadOnlyDictionary<int, Guid> rooms,
        Instant startedAt)
    {
        public ScriptedInstance Entrance { get; } = entrance;

        public IReadOnlyDictionary<int, Guid> Rooms { get; } = rooms;

        public Instant StartedAt { get; } = startedAt;

        public byte LivesRemaining { get; set; } = entrance.Definition?.Lives ?? 0;

        public short ReturnMapId { get; } = entrance.MapId;

        public short ReturnX { get; } = entrance.PositionX;

        public short ReturnY { get; } = entrance.PositionY;
    }
}
