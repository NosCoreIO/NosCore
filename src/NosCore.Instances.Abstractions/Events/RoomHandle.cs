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
        private readonly List<(TimeSpan Interval, Action<RoomEvent> Step)> _waves = [];
        private readonly List<ButtonHandle> _buttons = [];
        private readonly HashSet<PortalHandle> _portals = [];
        private Action<RoomEvent>? _entered;
        private Action<RoomEvent>? _cleared;
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
            var spawns = npcs.Select(npc => NpcSpec.Of(npc).Spawn()).ToArray();
            script.Setup(run => run.SummonNpcs(Key, spawns));
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

        public IButton Sculpture(Spot at, short offVNum, short onVNum) => Button(at, offVNum, onVNum, ButtonKind.Sculpture);

        private ButtonHandle Button(Spot at, short offVNum, short onVNum, ButtonKind kind)
        {
            var button = new ButtonHandle(_buttons.Count + 1);
            _buttons.Add(button);
            script.Setup(run => run.PlaceButton(Key, button.Id, at, offVNum, onVNum, kind: kind));
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

        public IRoom Every(TimeSpan interval, Action<RoomEvent> step)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
            _waves.Add((interval, step));
            return this;
        }

        public IRoom OnClockOut(Action<RoomEvent> step)
        {
            _clockOut += step;
            return this;
        }

        internal void Entered(IInstanceRun run) => _entered?.Invoke(new RoomEvent(run, this));

        internal void Cleared(IInstanceRun run) => _cleared?.Invoke(new RoomEvent(run, this));

        internal void DialogClosed(IInstanceRun run, int dialog) =>
            _dialogsClosed.GetValueOrDefault(dialog)?.Invoke(new RoomEvent(run, this));

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
            foreach (var (interval, step) in _waves)
            {
                Wave(run, startedAt, interval, step, clock);
            }
        }

        // A wave that comes due as the clock runs out, or is only reached after it has, does not run.
        private void Wave(IInstanceRun run, TimeSpan at, TimeSpan interval, Action<RoomEvent> step, RoomClock clock)
        {
            if (run.RoomClockOf(Key) != clock || run.Elapsed >= clock.End)
            {
                return;
            }

            if (run.PlayersIn(Key) > 0)
            {
                step(new RoomEvent(run, this));
            }

            run.At(at + interval, next => Wave(next, at + interval, interval, step, clock));
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
