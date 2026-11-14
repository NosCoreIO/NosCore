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
        TimeSpan Elapsed { get; }

        void Summon(int room, params MonsterSpawn[] monsters);

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
