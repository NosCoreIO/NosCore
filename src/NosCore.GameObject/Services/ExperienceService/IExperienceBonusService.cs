//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.ExperienceService
{
    public interface IExperienceBonusService
    {
        ExperienceBonus GetBonus(PlayerComponentBundle player);
    }
}
