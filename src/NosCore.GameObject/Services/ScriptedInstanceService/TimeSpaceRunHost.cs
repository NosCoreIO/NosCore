//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class TimeSpaceRunHost(ScriptedInstance entrance, Lazy<IMapChangeService> mapChangeService,
        IMessageBus messageBus, IClock clock)
        : IInstanceRunHost
    {
        private static readonly Duration ReviveDelay = Duration.FromSeconds(10);

        private readonly Dictionary<long, Instant> _revivesAt = [];

        private int _livesLost;

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

        public async Task ReturnAsync(ClientSession session)
        {
            if (!session.Character.IsAlive)
            {
                await session.Character.ReviveAsync(fully: false);
            }

            await mapChangeService.Value.ChangeMapAsync(session, entrance.MapId, entrance.PositionX, entrance.PositionY);
        }

        // Lives are shared by the whole run. False once the party is out of them, which fails the run.
        public bool LoseLife(ScriptedInstanceRun run)
        {
            _livesLost++;
            if (_livesLost <= (entrance.Definition?.Lives ?? 0))
            {
                return true;
            }

            run.RunOutOfLives();
            return false;
        }

        public async Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player)
        {
            if (!LoseLife(run))
            {
                return;
            }

            _revivesAt[player.CharacterId] = clock.GetCurrentInstant().Plus(ReviveDelay);
            await player.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.YouCanBeRevivedMoreTime,
                ArgumentType = 4,
                Game18NArguments = { (entrance.Definition?.Lives ?? 0) - _livesLost }
            });
            await player.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.ReviveSoon });
        }

        public async Task TickAsync(ScriptedInstanceRun run)
        {
            var now = clock.GetCurrentInstant();
            foreach (var (characterId, _) in _revivesAt.Where(revive => revive.Value <= now).ToList())
            {
                _revivesAt.Remove(characterId);
                if (run.Players().FirstOrDefault(s => s.HasPlayerEntity && s.Character.CharacterId == characterId) is { } session)
                {
                    run.StoodUp(characterId);
                    await session.Character.ReviveAsync(fully: true);
                }
            }
        }
    }
}
