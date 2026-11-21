//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Instances.Abstractions
{
    // A time-space declared in its constructor: its rooms, what they hold and the steps their events run.
    // The run's triggers are dispatched to the object they belong to, so a script never tests an id.
    public abstract class TimeSpaceScript : IInstanceScript
    {
        private readonly InstanceDefinitionBuilder _builder;
        private readonly List<RoomHandle> _rooms = [];
        private readonly List<Action<IInstanceRun>> _setup = [];
        private readonly List<(TimeSpan At, Action<RunEvent> Step)> _timers = [];
        private Action<RunEvent>? _timeout;
        private TimeSpan? _clock;
        private ScriptedInstanceDefinition? _definition;

        protected TimeSpaceScript(short id, string title) =>
            _builder = InstanceDefinitionBuilder.Named(id, title, title);

        public ScriptedInstanceDefinition Definition => _definition ??= _builder.Build();

        protected void Levels(byte minimum, byte maximum) => _builder.ForLevels(minimum, maximum);

        protected void Start(short x, short y) => _builder.StartingAt(x, y);

        protected void Requires(short vNum, short amount) => _builder.Requiring(vNum, amount);

        protected void Lives(byte lives) => _builder.WithLives(lives);

        // The run's clock, started as the first player enters.
        protected void Clock(TimeSpan limit) => _clock = limit;

        // A goal of the mission window: the exit only lets the party out once every one is met.
        protected void Objective(ObjectiveKind kind, short vNum, short count) => _builder.WithObjective(kind, vNum, count);

        protected void Reward(short vNum, short amount) => _builder.WithReward(vNum, amount);

        protected void Draw(short vNum, short amount) => _builder.WithDraw(vNum, amount);

        protected void Special(short vNum, short amount) => _builder.WithSpecialReward(vNum, amount);

        // Its place on the mission map.
        protected IRoom Room(short mapVNum, byte x, byte y)
        {
            _builder.WithRoom(mapVNum, out var key, x, y);
            var room = new RoomHandle(this, key);
            _rooms.Add(room);
            return room;
        }

        protected static IMonster Monster(short vNum) => new MonsterSpec(vNum);

        protected static INpc Npc(short vNum) => new NpcSpec(vNum);

        protected static IItem Item(short vNum) => new ItemSpec(vNum);

        // That long after the run started.
        protected void After(TimeSpan elapsed, Action<RunEvent> step) => _timers.Add((elapsed, step));

        // Once every one of the rooms has been cleared, as the last of them is, whatever the order.
        protected void OnAllCleared(IRoom[] rooms, Action<RunEvent> step)
        {
            var handles = rooms.Select(RoomHandle.Of).Distinct().ToList();
            foreach (var room in handles)
            {
                room.OnCleared(run =>
                {
                    if (!run.WasCleared(room.Key) && handles.All(other => other == room || run.WasCleared(other.Key)))
                    {
                        step(new RunEvent(run));
                    }
                });
            }
        }

        // Before the run fails for running out of time; a step that ends the run another way stands.
        protected void OnTimeout(Action<RunEvent> step) => _timeout += step;

        internal void Setup(Action<IInstanceRun> step) => _setup.Add(step);

        void IInstanceScript.OnFirstEnter(IInstanceRun run)
        {
            if (_clock is { } limit)
            {
                run.StartClock(limit);
            }

            foreach (var step in _setup)
            {
                step(run);
            }

            foreach (var (at, step) in _timers)
            {
                run.At(at, timer => step(new RunEvent(timer)));
            }
        }

        // A monster's own OnDeath answers for its fall.
        void IInstanceScript.OnMonsterKilled(IInstanceRun run, int room, short vNum)
        {
        }

        void IInstanceScript.OnRoomCleared(IInstanceRun run, int room) => RoomOf(room)?.Cleared(run);

        void IInstanceScript.OnRoomEntered(IInstanceRun run, int room) => RoomOf(room)?.Entered(run);

        void IInstanceScript.OnButtonUsed(IInstanceRun run, int room, int button) => RoomOf(room)?.ButtonUsed(run, button);

        void IInstanceScript.OnDialogClosed(IInstanceRun run, int room, int dialog) => RoomOf(room)?.DialogClosed(run, dialog);

        void IInstanceScript.OnItemCollected(IInstanceRun run, int room, short vNum) => RoomOf(room)?.ItemCollected(run, vNum);

        // Reaching the way out is how a time-space is won.
        void IInstanceScript.OnExitReached(IInstanceRun run, int room)
        {
            RoomOf(room)?.ExitReached(run);
            run.Succeed();
        }

        void IInstanceScript.OnTimeout(IInstanceRun run) => _timeout?.Invoke(new RunEvent(run));

        private RoomHandle? RoomOf(int key) => _rooms.Find(room => room.Key == key);
    }
}
