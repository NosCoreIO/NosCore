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
    // Holds only what the script declared, which every run shares; a run's own state stays in the run.
    internal sealed class RoomHandle(TimeSpaceScript script, int key) : IRoom
    {
        private const short LeverOff = 1000;
        private const short LeverOn = 1045;

        private readonly Dictionary<int, Action<RoomEvent>> _dialogsClosed = [];
        private readonly Dictionary<short, Action<RoomEvent>> _itemsCollected = [];
        private readonly List<(TimeSpan Interval, TimeSpan From, int? Times, Action<RoomEvent> Step)> _waves = [];
        private readonly List<ButtonHandle> _buttons = [];
        private Action<RoomEvent>? _entered;
        private Action<RoomEvent>? _cleared;
        private Action<IInstanceRun>? _clearedForRun;
        private Action<RoomEvent>? _clockOut;
        private ExitHandle? _exit;

        public int Key => key;

        internal static RoomHandle Of(IRoom room) =>
            room as RoomHandle ?? throw new ArgumentException($"{room} is not a room of this script.", nameof(room));

        public IRoom Spawn(params IMonster[] monsters)
        {
            var specs = MonsterSpec.All(monsters);
            script.Setup(run => SummonAll(run, specs));
            return this;
        }

        public IRoom Spawn(params INpc[] npcs)
        {
            var spawns = npcs.Select(npc => NpcSpec.Of(npc).Spawn(this)).ToArray();
            script.Setup(run => run.SummonNpcs(Key, spawns));
            return this;
        }

        public IRoom Spawn(params IItem[] items)
        {
            var specs = items.Select(ItemSpec.Of).ToList();
            script.Setup(run => specs.ForEach(item => run.PlaceItem(Key, item.VNum, item.Cell)));
            return this;
        }

        public IPortal Portal(Spot at, IRoom to, Spot arrival, bool locked = true)
        {
            var portal = new PortalHandle(Key, Of(to).Key);
            script.Setup(run => run.SpawnPortal(portal.From, at, portal.To, arrival, locked));
            return portal;
        }

        public IButton Lever(Spot at)
        {
            var button = new ButtonHandle(_buttons.Count + 1);
            _buttons.Add(button);
            script.Setup(run => run.PlaceButton(Key, button.Id, at, LeverOff, LeverOn));
            return button;
        }

        public IExit Exit(Spot at, bool locked = true)
        {
            if (_exit != null)
            {
                throw new InvalidOperationException($"Room {Key} already has an exit.");
            }

            _exit = new ExitHandle(this);
            script.Setup(run => run.SpawnExit(Key, at, locked));
            return _exit;
        }

        public IRoom OnEnter(Action<RoomEvent> step)
        {
            _entered += step;
            return this;
        }

        public IRoom OnCleared(Action<RoomEvent> step)
        {
            _cleared += step;
            return this;
        }

        public IRoom OnDialogClosed(int dialog, Action<RoomEvent> step)
        {
            _dialogsClosed[dialog] = _dialogsClosed.GetValueOrDefault(dialog) + step;
            return this;
        }

        public IRoom Every(TimeSpan interval, Action<RoomEvent> step, TimeSpan from = default, int? times = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfLessThan(from, TimeSpan.Zero);
            if (times is { } limit)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1, nameof(times));
            }

            _waves.Add((interval, from, times, step));
            return this;
        }

        public IRoom OnClockOut(Action<RoomEvent> step)
        {
            _clockOut += step;
            return this;
        }

        public IRoom OnItemCollected(short vNum, Action<RoomEvent> step)
        {
            _itemsCollected[vNum] = _itemsCollected.GetValueOrDefault(vNum) + step;
            return this;
        }

        internal void Entered(IInstanceRun run) => _entered?.Invoke(new RoomEvent(run, this));

        internal void Cleared(IInstanceRun run)
        {
            _cleared?.Invoke(new RoomEvent(run, this));
            _clearedForRun?.Invoke(run);
        }

        internal void OnCleared(Action<IInstanceRun> step) => _clearedForRun += step;

        internal void DialogClosed(IInstanceRun run, int dialog) =>
            _dialogsClosed.GetValueOrDefault(dialog)?.Invoke(new RoomEvent(run, this));

        internal void ItemCollected(IInstanceRun run, short vNum) =>
            _itemsCollected.GetValueOrDefault(vNum)?.Invoke(new RoomEvent(run, this));

        internal void ButtonUsed(IInstanceRun run, int button)
        {
            if (button >= 1 && button <= _buttons.Count)
            {
                _buttons[button - 1].Used(new RoomEvent(run, this));
            }
        }

        internal void ExitReached(IInstanceRun run) => _exit?.Reached(new RoomEvent(run, this));

        internal void Summon(IInstanceRun run, IMonster[] monsters) => SummonAll(run, MonsterSpec.All(monsters));

        private void SummonAll(IInstanceRun run, IReadOnlyList<MonsterSpec> specs) =>
            run.Summon(Key, specs.SelectMany(spec => spec.Spawns(run, this)).ToArray());

        internal void StartClock(IInstanceRun run, TimeSpan length)
        {
            run.StartRoomClock(Key, length);
            var clock = new RoomClock();
            var startedAt = run.Elapsed;
            run.At(startedAt + length, over =>
            {
                clock.Running = false;
                _clockOut?.Invoke(new RoomEvent(over, this));
                over.StopRoomClock(Key);
            });
            foreach (var (interval, from, times, step) in _waves)
            {
                var wave = new RoomWave(interval, times ?? int.MaxValue, step, clock);
                if (from == TimeSpan.Zero)
                {
                    Wave(run, startedAt, wave, 1);
                }
                else
                {
                    run.At(startedAt + from, first => Wave(first, startedAt + from, wave, 1));
                }
            }
        }

        // A wave due as the clock runs out was scheduled after the clock's end, so it finds the clock stopped.
        private void Wave(IInstanceRun run, TimeSpan at, RoomWave wave, int call)
        {
            if (!wave.Clock.Running)
            {
                return;
            }

            if (run.PlayersIn(Key) > 0)
            {
                wave.Step(new RoomEvent(run, this));
            }

            if (call < wave.Times)
            {
                run.At(at + wave.Interval, next => Wave(next, at + wave.Interval, wave, call + 1));
            }
        }

        private sealed record RoomWave(TimeSpan Interval, int Times, Action<RoomEvent> Step, RoomClock Clock);

        // Created when a run starts the clock and held only by the steps that run schedules, so each run has its own.
        private sealed class RoomClock
        {
            public bool Running { get; set; } = true;
        }
    }

    internal sealed record PortalHandle(int From, int To) : IPortal
    {
        internal static PortalHandle Of(IPortal portal) =>
            portal as PortalHandle ?? throw new ArgumentException($"{portal} is not a portal of this script.", nameof(portal));
    }

    internal sealed class ButtonHandle(int id) : IButton
    {
        private Action<RoomEvent>? _used;

        public int Id => id;

        public IButton OnUse(Action<RoomEvent> step)
        {
            _used += step;
            return this;
        }

        internal void Used(RoomEvent room) => _used?.Invoke(room);
    }

    internal sealed class ExitHandle(RoomHandle room) : IExit
    {
        private Action<RoomEvent>? _reached;

        public RoomHandle Room => room;

        internal static ExitHandle Of(IExit exit) =>
            exit as ExitHandle ?? throw new ArgumentException($"{exit} is not an exit of this script.", nameof(exit));

        public IExit OnReached(Action<RoomEvent> step)
        {
            _reached += step;
            return this;
        }

        internal void Reached(RoomEvent room) => _reached?.Invoke(room);
    }
}
