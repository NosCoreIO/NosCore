//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.TalentArenaService
{
    public sealed record TalentArenaFighter(long CharacterId, ArenaTeamType Team, byte Order, bool InRing,
        bool Defeated, bool Called);
}
