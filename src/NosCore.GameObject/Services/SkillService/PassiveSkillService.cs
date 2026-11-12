//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Services.SkillService
{
    public interface IPassiveSkillService
    {
        /// <summary>What the character's training courses and tomes add, for as long as they are known.</summary>
        IReadOnlyList<BCardDto> GetBCards(ICharacterEntity character);
    }

    public sealed class PassiveSkillService(INpcCombatCatalog catalog) : IPassiveSkillService, ISingletonService
    {
        public IReadOnlyList<BCardDto> GetBCards(ICharacterEntity character) =>
            character.Skills.Values
                .Where(s => s.Skill.IsPassive())
                .SelectMany(s => catalog.GetSkillBCards(s.SkillVNum))
                .ToList();
    }
}
