//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.BattleService.Model;

public enum HitStatus
{
    Landed,
    Cancelled,
    Missed,
}

// Result of a single hit after queue processing. `Status == Landed` implies Damage was
// actually subtracted from the target; killed == target HP reached 0.
public sealed record HitOutcome(
    HitStatus Status,
    int Damage,
    SuPacketHitMode HitMode,
    bool Killed);
