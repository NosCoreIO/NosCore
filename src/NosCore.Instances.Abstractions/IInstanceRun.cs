//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using System;

namespace NosCore.Instances.Abstractions
{
    public readonly record struct Spot(short X, short Y);

    public readonly record struct MonsterSpawn(short VNum, Spot At, bool IsHostile = true);

    // The verbs a script may use. Everything a script can do to a running instance goes
    // through here, so the engine behind it - csx today - stays swappable.
    public interface IInstanceRun
    {
        // Time since the script started, which is when its first player finished entering.
        TimeSpan Elapsed { get; }

        // Runs the step once the script has been going for that long, unless the run has ended.
        void At(TimeSpan elapsed, Action<IInstanceRun> step);

        void Summon(int room, params MonsterSpawn[] monsters);

        // Brings in the monsters the instance's own data lists for that wave, scattered over the room.
        void SummonWave(int room, byte wave);

        // Scatters the items the instance's own data lists for that wave over the room.
        void DropWave(int room, byte wave);

        int MonstersAlive(int room);

        int PlayersIn(int room);

        // True once the players in the room who are still in the fight are all on one side,
        // a group counting as one side.
        bool OneSideStanding(int room);

        void SetPvp(int room, bool isPvp);

        // Brings the monster in beside the player who entered the room last and sets it on them.
        void Hunt(int room, short vNum);

        // Takes that monster's living copies out of the room.
        void Dismiss(int room, short vNum);

        // Multiplies what the room's monsters give in experience and drops.
        void SetRates(int room, int experience, int drops);

        void SpawnPortal(int fromRoom, Spot at, int toRoom, Spot arrival, bool locked = true);

        void UnlockPortal(int fromRoom, int toRoom);

        // A locker holds the room shut until that many of its monsters are dead.
        void SetMonsterLocker(int room, short count);

        void StartClock(TimeSpan limit);

        void Message(Game18NConstString message);

        void Succeed();

        void Fail();
    }

    // One script object serves every run of its file, so per-run state belongs to the run.
    public interface IInstanceScript
    {
        ScriptedInstanceDefinition Definition { get; }

        void OnFirstEnter(IInstanceRun run);

        void OnMonsterKilled(IInstanceRun run, int room, short vNum);

        void OnRoomCleared(IInstanceRun run, int room);

        void OnTimeout(IInstanceRun run);
    }

    // Scripts subclass this so they only override the triggers they use.
    public abstract class InstanceScript : IInstanceScript
    {
        public ScriptedInstanceDefinition Definition { get; protected init; } = new();

        public virtual void OnFirstEnter(IInstanceRun run) { }

        public virtual void OnMonsterKilled(IInstanceRun run, int room, short vNum) { }

        public virtual void OnRoomCleared(IInstanceRun run, int room) { }

        public virtual void OnTimeout(IInstanceRun run) { }
    }
}
