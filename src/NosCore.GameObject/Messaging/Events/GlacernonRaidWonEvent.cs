//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;

namespace NosCore.GameObject.Messaging.Events
{
    // Published once when a family's run of a Glacernon raid beats its boss.
    public sealed record GlacernonRaidWonEvent(long FamilyId, Act4RaidType Raid, bool InTenMinutes, bool WithoutLoss);
}
