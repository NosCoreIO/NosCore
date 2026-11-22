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
        private readonly Dictionary<int, Action<RoomEvent>> _dialogsClosed = [];
        private readonly Dictionary<short, Action<RoomEvent>> _itemsCollected = [];
        private readonly List<(TimeSpan Interval, TimeSpan From, int? Times, Action<RoomEvent> Step)> _waves = [];
        private readonly List<Action<IInstanceRun>> _buttons = [];
        private readonly HashSet<PortalHandle> _portals = [];
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
            if (!_portals.Add(portal))
            {
                throw new InvalidOperationException($"Room {Key} already has a portal to room {portal.To}.");
            }

            script.Setup(run => run.SpawnPortal(portal.From, at, portal.To, arrival, locked));
            return portal;
        }

        public IButton Lever(Spot at, short offVNum = 1000, short onVNum = 1045) => Button(at, offVNum, onVNum, ButtonKind.Lever);

        public ISwitch Switch(Spot at, short offVNum, short onVNum)
        {
            var handle = new SwitchHandle(_buttons.Count + 1);
            _buttons.Add(run => handle.Used(run, this));
            script.Setup(run => run.PlaceButton(Key, handle.Id, at, offVNum, onVNum, toggles: true));
            return handle;
        }

        public IButton Sculpture(Spot at, short offVNum, short onVNum) => Button(at, offVNum, onVNum, ButtonKind.Sculpture);

        public IButton KeyAnywhere(short vNum) => Button(run => run.RandomCell(Key), vNum, vNum, ButtonKind.Key);

        private ButtonHandle Button(Spot at, short offVNum, short onVNum, ButtonKind kind) => Button(_ => at, offVNum, onVNum, kind);

        private ButtonHandle Button(Func<IInstanceRun, Spot> at, short offVNum, short onVNum, ButtonKind kind)
        {
            var button = new ButtonHandle(_buttons.Count + 1);
            _buttons.Add(run => button.Used(new RoomEvent(run, this)));
            script.Setup(run => run.PlaceButton(Key, button.Id, at(run), offVNum, onVNum, kind: kind));
            return button;
        }

        public IRoom ClearedAfter(short kills, short vNum)
        {
            script.Setup(run => run.SetMonsterLocker(Key, kills, vNum));
            return this;
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
                _buttons[button - 1](run);
            }
        }

        internal void ExitReached(IInstanceRun run) => _exit?.Reached(new RoomEvent(run, this));

        internal void Summon(IInstanceRun run, IMonster[] monsters) => SummonAll(run, MonsterSpec.All(monsters));

        private void SummonAll(IInstanceRun run, IReadOnlyList<MonsterSpec> specs) =>
            run.Summon(Key, specs.SelectMany(spec => spec.Spawns(run, this)).ToArray());

        // The run numbers the clocks it starts in a room, so the waves and the end of a clock that was
        // stopped or started again find another one running and do nothing, and a clock started by
        // OnClockOut is not taken down by the end of the clock it follows.
        internal void StartClock(IInstanceRun run, TimeSpan length)
        {
            var startedAt = run.Elapsed;
            run.StartRoomClock(Key, length);
            var clock = run.RoomClockOf(Key) ?? new RoomClock(0, startedAt + length);
            run.At(clock.End, over =>
            {
                if (over.RoomClockOf(Key) != clock)
                {
                    return;
                }

                _clockOut?.Invoke(new RoomEvent(over, this));
                if (over.RoomClockOf(Key) == clock)
                {
                    over.StopRoomClock(Key);
                }
            });
            foreach (var (interval, from, times, step) in _waves)
            {
                var wave = new RoomWave(interval, times ?? int.MaxValue, step, clock);
                if (from == TimeSpan.Zero)
                {
                    Wave(run, startedAt, wave, 0);
                }
                else
                {
                    run.At(startedAt + from, first => Wave(first, startedAt + from, wave, 0));
                }
            }
        }

        // A wave that comes due as the clock runs out, or is only reached after it has, does not run.
        // An interval that finds the room empty calls nothing, so it does not count towards Times.
        private void Wave(IInstanceRun run, TimeSpan at, RoomWave wave, int calls)
        {
            if (run.RoomClockOf(Key) != wave.Clock || run.Elapsed >= wave.Clock.End)
            {
                return;
            }

            if (run.PlayersIn(Key) > 0)
            {
                wave.Step(new RoomEvent(run, this));
                calls++;
            }

            if (calls < wave.Times)
            {
                run.At(at + wave.Interval, next => Wave(next, at + wave.Interval, wave, calls));
            }
        }

        private sealed record RoomWave(TimeSpan Interval, int Times, Action<RoomEvent> Step, RoomClock Clock);
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

    internal sealed class SwitchHandle(int id) : ISwitch
    {
        private Action<RoomEvent>? _switchedOn;
        private Action<RoomEvent>? _firstSwitchedOn;
        private Action<RoomEvent>? _switchedOff;

        public int Id => id;

        public ISwitch OnSwitchedOn(Action<RoomEvent> step)
        {
            _switchedOn += step;
            return this;
        }

        public ISwitch OnFirstSwitchedOn(Action<RoomEvent> step)
        {
            _firstSwitchedOn += step;
            return this;
        }

        public ISwitch OnSwitchedOff(Action<RoomEvent> step)
        {
            _switchedOff += step;
            return this;
        }

        internal void Used(IInstanceRun run, RoomHandle room)
        {
            var used = new RoomEvent(run, room);
            if (!run.IsButtonOn(room.Key, id))
            {
                _switchedOff?.Invoke(used);
                return;
            }

            _switchedOn?.Invoke(used);
            if (run.TimesSwitchedOn(room.Key, id) == 1)
            {
                _firstSwitchedOn?.Invoke(used);
            }
        }
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
