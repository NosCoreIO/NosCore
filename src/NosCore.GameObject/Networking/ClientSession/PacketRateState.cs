//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;

namespace NosCore.GameObject.Networking.ClientSession;

// Token bucket for every packet a session sends, before a character is even selected.
public sealed class PacketRateState
{
    public object Lock { get; } = new();

    public bool Started { get; set; }

    public double Tokens { get; set; }

    public Instant LastRefill { get; set; }

    public Instant DropWindowStart { get; set; }

    public int DroppedInWindow { get; set; }
}
