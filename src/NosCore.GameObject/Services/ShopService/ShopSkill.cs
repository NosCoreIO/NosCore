//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.ShopService
{
    public class ShopSkill : ShopSkillDto
    {
        public required SkillDto Skill { get; set; }
    }
}
