//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class TimeSpaceRunHost(ScriptedInstance entrance, Lazy<IMapChangeService> mapChangeService,
        IMessageBus messageBus)
        : IInstanceRunHost
    {
        public ScriptedInstance Entrance => entrance;

        public string Name => $"scripted instance {entrance.ScriptedInstanceId}";

        public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

        public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

        public async Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
        {
            await run.BroadcastAsync(new ScorePacket
            {
                EndState = state,
                MonsterKilled = (short)run.MonstersKilled,
                RoomsVisited = (short)run.RoomsVisited,
                IsAllMonstersKilled = run.MonstersKilled >= run.MonstersSummoned,
                IsNoNpcDead = true
            });

            if (state != EndStateType.Success || entrance.Definition is not { } definition)
            {
                return;
            }

            foreach (var session in run.Players().Where(s => s.HasPlayerEntity).ToList())
            {
                await messageBus.PublishAsync(new ScriptedInstanceSucceededEvent(session.Character, entrance.Type, definition.Id));
            }
        }

        public Task ReturnAsync(ClientSession session) =>
            mapChangeService.Value.ChangeMapAsync(session, entrance.MapId, entrance.PositionX, entrance.PositionY);
    }
}
