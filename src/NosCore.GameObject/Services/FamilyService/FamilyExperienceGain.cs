//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Services.FamilyService
{
    public record FamilyExperienceGain(byte FamilyLevel, bool LeveledUp)
    {
        public IReadOnlyList<short> CompletedQuestIds { get; init; } = [];
    }
}
