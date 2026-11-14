//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Portals;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FactionWarService;

public interface IFactionWarService
{
    // Hundredths of a percent, 0 to 10000.
    int Progress(FactionType faction);

    Act4WarMode Mode(FactionType faction);

    Act4RaidType? OpenRaid(FactionType faction);

    Instant? RaidClosesAt(FactionType faction);

    FcPacket GenerateFc(PlayerComponentBundle player);

    // The gate to the player's side's raid, while it is open and the player stands on its map.
    GpPacket? GenerateRaidGate(PlayerComponentBundle player);

    // Returns false when the two players are not enemies, so the kill counts for nothing.
    Task<bool> RecordKillAsync(PlayerComponentBundle killer, PlayerComponentBundle victim);

    // True when the monster was a side's Lord Mukraju, whose death opens that side's raid.
    Task<bool> GuardianDiedAsync(MonsterComponentBundle monster);

    Task TickAsync();
}
