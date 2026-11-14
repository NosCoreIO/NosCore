//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.MateService
{
    public interface IPetSkillBarService
    {
        /// <summary>The skills of a pet's skill bar, in slot order: its creature's own; none for a partner.</summary>
        IReadOnlyList<SkillDto> Skills(Mate pet);

        /// <summary>Shows the owner the skill bar of the team pet at their side, or no bar when there is none.</summary>
        Task RefreshAsync(PlayerComponentBundle owner);
    }
}
