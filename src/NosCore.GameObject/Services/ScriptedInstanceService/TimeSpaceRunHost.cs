//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class TimeSpaceRunHost(ScriptedInstance entrance, Lazy<IMapChangeService> mapChangeService)
        : IInstanceRunHost
    {
        public ScriptedInstance Entrance => entrance;

        public string Name => $"scripted instance {entrance.ScriptedInstanceId}";

        public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

        public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

        public Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
        {
            return run.BroadcastAsync(new ScorePacket
            {
                EndState = state,
                MonsterKilled = (short)run.MonstersKilled,
                RoomsVisited = (short)run.RoomsVisited,
                IsAllMonstersKilled = run.MonstersKilled >= run.MonstersSummoned,
                IsNoNpcDead = true
            });
        }

        public Task ReturnAsync(ClientSession session) =>
            mapChangeService.Value.ChangeMapAsync(session, entrance.MapId, entrance.PositionX, entrance.PositionY);
    }
}
