//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SkillService;

namespace NosCore.GameObject.Services.MateService
{
    public interface IPartnerSkillCatalog
    {
        /// <summary>The skills a partner card gives, in skill bar order.</summary>
        IReadOnlyList<SkillDto> CardSkills(SpecialistInstance card);
    }

    public sealed class PartnerSkillCatalog(List<SkillDto> skills) : IPartnerSkillCatalog, ISingletonService
    {
        private readonly ILookup<short, SkillDto> _skillsBySet = skills
            .Where(s => s.IsMateSkill())
            .OrderBy(s => s.CastId)
            .ToLookup(s => s.UpgradeType);

        public IReadOnlyList<SkillDto> CardSkills(SpecialistInstance card) => _skillsBySet[card.Item.PartnerSkillSet].ToList();
    }
}
