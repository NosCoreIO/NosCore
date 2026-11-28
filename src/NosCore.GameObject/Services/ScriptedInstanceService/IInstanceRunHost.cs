//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public readonly record struct WaveMonster(short VNum, short Amount, bool IsMoving);

    // What differs between the kinds of scripted instance: where their data comes from, what
    // an ending gives the players, and where the players go afterwards.
    public interface IInstanceRunHost
    {
        string Name { get; }

        IEnumerable<WaveMonster> WaveMonsters(byte wave);

        Task DropWaveAsync(MapInstance room, byte wave);

        Task EndedAsync(ScriptedInstanceRun run, EndStateType state);

        Task ReturnAsync(ClientSession session);

        // False when the host starts the script itself, for instance once a sign-up is over.
        bool StartsOnFirstEntry => true;

        // How far into its timeline the script begins, for a run that joins an event already under way.
        TimeSpan AlreadyElapsed => TimeSpan.Zero;

        // A player whose HP reached zero inside the run. They stay out of the fight until the run
        // ends, unless the host stands them up again.
        Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player) => Task.CompletedTask;

        // The player answered the revival dialog the host asked when they fell.
        Task RevivalAnsweredAsync(ScriptedInstanceRun run, PlayerComponentBundle player) => Task.CompletedTask;

        Task TickAsync(ScriptedInstanceRun run) => Task.CompletedTask;

        // The lives a run's players still share, shown in the mission window. Null for a host with no window.
        int? LivesLeft => null;

        // Whether the players leave the first room only once one of them confirms the start.
        bool AsksToStart => false;

        // Whether the client draws the run's rooms as a map that fills in as they are discovered.
        bool DrawsRoomMap => false;

        // Whether the players are shown a running score, which the kills and cleared rooms below add to.
        bool ShowsScore => false;

        // A chain counts the marked monsters killed in a row, this one included; 0 for an unmarked monster.
        int KillPoints(byte monsterLevel, byte killerLevel, int chain) => 0;

        int RoomClearedPoints => 0;

        // Whether the run counts the players' own hits on each monster, so the kill that wins it can rank them.
        bool RanksDamage => false;

        Task NpcPlacedAsync(ScriptedInstanceRun run, MapInstance room, NpcComponentBundle npc) => Task.CompletedTask;

        Task CountdownAsync(ScriptedInstanceRun run, MapInstance room, TimeSpan length) =>
            room.SendPacketAsync(new EventPacket
            {
                Type = EventPacketType.TSTimer,
                Parameter = (int)(length.TotalMilliseconds / 100),
                SecondParameter = (int)(length.TotalMilliseconds / 100),
            });

        // Bouts, for the hosts that keep fighters. These run inside a script step, so packets go
        // through run.Queue.
        bool CallFighters(ScriptedInstanceRun run, MapInstance room) => false;

        void OpenBout(ScriptedInstanceRun run, MapInstance room) { }

        int FightersStanding(ScriptedInstanceRun run, MapInstance room) => 0;

        void EndBout(ScriptedInstanceRun run, MapInstance room) { }
    }
}
