//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.MovementService;

// A cell takes 2500 / speed milliseconds to cross. Each accepted step is charged at the speed
// the character had when it took it, and the time since the last accepted step pays that back.
public static class WalkAllowance
{
    private const double MillisecondsPerCellAtSpeedOne = 2500;

    // Clocks on both ends drift and packets arrive in clumps.
    private const double RateTolerance = 4D / 3;

    private const double LatencyMilliseconds = 500;

    public static double Cost(int cells, byte speed) =>
        cells == 0 ? 0 : speed == 0 ? double.PositiveInfinity : cells * MillisecondsPerCellAtSpeedOne / speed;

    // One packet may carry the client up to speed / 2 cells ahead of where it stands.
    public static double Burst(byte speed) => speed == 0 ? 0 : Cost(speed / 2 + 1, speed) + LatencyMilliseconds;

    public static double OwedAfter(double owed, double elapsedMilliseconds, double cost) =>
        Math.Max(0, owed - Math.Max(0, elapsedMilliseconds) * RateTolerance) + cost;

    public static bool IsAffordable(double owed, byte speed) => owed <= Burst(speed);
}
