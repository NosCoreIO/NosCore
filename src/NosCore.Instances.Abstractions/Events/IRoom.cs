//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.Instances.Abstractions
{
    public interface IRoom
    {
        // What the room holds from the start of the run.
        IRoom Spawn(params IMonster[] monsters);

        IRoom Spawn(params INpc[] npcs);

        // A one-way portal; the way back is a portal of the other room. The run tells portals apart by the
        // rooms they join, so a room has at most one portal to each other room.
        IPortal Portal(Spot at, IRoom to, Spot arrival, bool locked = true);

        // Drawn as offVNum until it is used, then as onVNum; says it has been pulled.
        IButton Lever(Spot at, short offVNum = 1000, short onVNum = 1045);

        // Moved without a word when it is used.
        IButton Sculpture(Spot at, short offVNum, short onVNum);

        // A key on a walkable cell of the room picked anew for each run, taken off the map by whoever uses it.
        IButton KeyAnywhere(short vNum);

        // The room counts as cleared once that many of its monsters of that vnum have fallen.
        IRoom ClearedAfter(short kills, short vNum);

        // A room has at most one exit: the run cannot tell two of them apart.
        IExit Exit(Spot at, bool locked = true);

        // The first time anyone enters the room.
        IRoom OnEnter(Action<RoomEvent> step);

        // When the room's last monster falls.
        IRoom OnCleared(Action<RoomEvent> step);

        // The first time someone closes that dialog after the room opened it.
        IRoom OnDialogClosed(int dialog, Action<RoomEvent> step);

        // While the room's clock runs: as it starts, then every interval, whenever someone is inside.
        IRoom Every(TimeSpan interval, Action<RoomEvent> step);

        // When the room's clock runs out; the clock is taken down after the step.
        IRoom OnClockOut(Action<RoomEvent> step);
    }

    public interface IPortal;

    public interface IButton
    {
        IButton OnUse(Action<RoomEvent> step);
    }

    // Reaching an open exit wins the run once its own steps have run.
    public interface IExit
    {
        IExit OnReached(Action<RoomEvent> step);
    }

    // Each call returns a new monster, so one can be the base of several.
    public interface IMonster
    {
        IMonster At(short x, short y);

        // A walkable cell picked on each spawn, kept clear of the doors.
        IMonster Anywhere();

        IMonster Around(short x, short y, short radius);

        // Each copy is placed on its own, so copies sent Anywhere or Around may share a cell.
        IMonster Count(int copies);

        IMonster Bonus();

        IMonster OnDeath(Action<RoomEvent> step);

        // Each hit it survives; a hit that kills it runs OnDeath instead.
        IMonster OnHit(Action<RoomEvent> step);

        // Once, on the hit that takes its HP below that percentage of its maximum.
        IMonster OnHpBelow(byte percent, Action<RoomEvent> step);
    }

    public interface INpc
    {
        INpc At(short x, short y);

        // Joins the first player who comes near it and follows them; the run fails if it falls.
        INpc Escort();
    }
}
