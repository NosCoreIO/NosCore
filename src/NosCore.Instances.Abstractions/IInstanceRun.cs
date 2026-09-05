//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;

namespace NosCore.Instances.Abstractions
{
    public readonly record struct Spot(short X, short Y);

    public readonly record struct MonsterSpawn(short VNum, Spot At, bool IsBoss = false);

    // The verbs a script may use. Everything a script can do to a running instance goes
    // through here, so the engine behind it - csx today - stays swappable.
    public interface IInstanceRun
    {
        int CurrentRoom { get; }
        TimeSpan Elapsed { get; }

        void Summon(int room, params MonsterSpawn[] monsters);
        void SummonNpc(int room, short vNum, Spot at);
        void SpawnPortal(int fromRoom, int toRoom, Spot at, bool locked = true);
        void UnlockPortal(int fromRoom, int toRoom);

        // A locker holds the room shut until the monsters it names are dead.
        void SetMonsterLocker(int room, short count);
        void SetButtonLocker(int room, short count);

        void StartClock(TimeSpan limit);
        void StopClock();

        void Message(string text);
        void Effect(int room, short effectId);

        void Succeed();
        void Fail();
    }

    public interface IInstanceScript
    {
        void OnFirstEnter(IInstanceRun run);
        void OnMonsterKilled(IInstanceRun run, short vNum);
        void OnRoomCleared(IInstanceRun run, int room);
        void OnTimeout(IInstanceRun run);
    }

    // Scripts subclass this so they only override the triggers they use, the way real
    // instance definitions omit whatever they do not have.
    public abstract class InstanceScript : IInstanceScript
    {
        public virtual void OnFirstEnter(IInstanceRun run) { }
        public virtual void OnMonsterKilled(IInstanceRun run, short vNum) { }
        public virtual void OnRoomCleared(IInstanceRun run, int room) { }
        public virtual void OnTimeout(IInstanceRun run) { }
    }
}
