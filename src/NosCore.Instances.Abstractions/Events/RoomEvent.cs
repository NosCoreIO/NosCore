//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;

namespace NosCore.Instances.Abstractions
{
    // What a step may do while one of its events runs. Each verb returns the event so they chain.
    public abstract class InstanceEvent<TSelf> where TSelf : InstanceEvent<TSelf>
    {
        private protected InstanceEvent(IInstanceRun run) => Run = run;

        private protected IInstanceRun Run { get; }

        // Shown to everyone in the run.
        public TSelf Say(Game18NConstString message)
        {
            Run.Message(message);
            return (TSelf)this;
        }

        public TSelf Say(LanguageKey message, params object[] arguments)
        {
            Run.Message(message, arguments);
            return (TSelf)this;
        }

        public TSelf Unlock(params IPortal[] portals)
        {
            foreach (var portal in portals)
            {
                var (from, to) = PortalHandle.Of(portal);
                Run.UnlockPortal(from, to);
            }

            return (TSelf)this;
        }

        public TSelf Unlock(IExit exit)
        {
            Run.UnlockExit(ExitHandle.Of(exit).Room.Key);
            return (TSelf)this;
        }

        // Scaled like the clock itself, so twice as much in Loser Mode.
        public TSelf AddTime(TimeSpan extra)
        {
            Run.AddClockTime(extra);
            return (TSelf)this;
        }

        public TSelf Succeed()
        {
            Run.Succeed();
            return (TSelf)this;
        }

        public TSelf Fail()
        {
            Run.Fail();
            return (TSelf)this;
        }
    }

    public sealed class RoomEvent : InstanceEvent<RoomEvent>
    {
        private readonly RoomHandle _room;

        internal RoomEvent(IInstanceRun run, RoomHandle room) : base(run) => _room = room;

        public RoomEvent Spawn(params IMonster[] monsters)
        {
            _room.Summon(Run, monsters);
            return this;
        }

        // Opens that NPC dialog for the players in the room; the run's clock waits while it is up.
        public RoomEvent Dialog(int dialog)
        {
            Run.Dialog(_room.Key, dialog);
            return this;
        }

        // The room's line in the mission panel until the room is cleared.
        public RoomEvent Objective(LanguageKey objective)
        {
            Run.Objective(_room.Key, objective);
            return this;
        }

        // The room's red clock, which runs the room's Every and OnClockOut steps.
        public RoomEvent StartRoomClock(TimeSpan length)
        {
            _room.StartClock(Run, length);
            return this;
        }

        // Takes the clock down before it runs out, so its waves and OnClockOut steps do not come.
        public RoomEvent StopRoomClock()
        {
            Run.StopRoomClock(_room.Key);
            return this;
        }

        public RoomEvent Spawn(params INpc[] npcs)
        {
            Run.SummonNpcs(_room.Key, npcs.Select(npc => NpcSpec.Of(npc).Spawn()).ToArray());
            return this;
        }

        // Takes the living copies of those monsters out of the room.
        public RoomEvent Dismiss(params short[] vNums)
        {
            foreach (var vNum in vNums)
            {
                Run.Dismiss(_room.Key, vNum);
            }

            return this;
        }

        // Shown only to the players standing in the room.
        public RoomEvent SayToRoom(Game18NConstString message)
        {
            Run.Message(_room.Key, message);
            return this;
        }

        public RoomEvent SayToRoom(LanguageKey message, params object[] arguments)
        {
            Run.Message(_room.Key, message, arguments);
            return this;
        }

        // That long after this event, in the same room.
        public RoomEvent After(TimeSpan delay, Action<RoomEvent> step)
        {
            Run.At(Run.Elapsed + delay, later => step(new RoomEvent(later, _room)));
            return this;
        }

        // Only when someone is standing in the room as the event runs.
        public RoomEvent IfAnyoneInside(Action<RoomEvent> step)
        {
            if (Run.PlayersIn(_room.Key) > 0)
            {
                step(this);
            }

            return this;
        }

        public RoomEvent In(IRoom room) => new(Run, RoomHandle.Of(room));
    }

    public sealed class RunEvent : InstanceEvent<RunEvent>
    {
        internal RunEvent(IInstanceRun run) : base(run)
        {
        }

        public RoomEvent In(IRoom room) => new(Run, RoomHandle.Of(room));
    }
}
