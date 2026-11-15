//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Messaging.Events
{
    // Published once per won raid run, with every player still inside who belongs to a family. RaidId is the
    // client's id for the raid, as the quest data uses it.
    public sealed record RaidWonEvent(IReadOnlyCollection<RaidWinner> Winners, short RaidId, bool IsHeroic);

    public sealed record RaidWinner(long FamilyId, long CharacterId);
}
