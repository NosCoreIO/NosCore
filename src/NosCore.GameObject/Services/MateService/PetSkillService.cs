//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.SkillService;
using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class PetSkillService(IClock clock, List<SkillDto> skills, INpcCombatCatalog catalog,
        IBattleService battleService, IHeuristic distanceCalculator) : IPetSkillService, ISingletonService
    {
        private const int SkillBarSlots = 3;

        private readonly Dictionary<short, SkillDto> _skillsByVnum = skills.ToDictionary(s => s.SkillVNum);

        public IReadOnlyList<SkillDto> Skills(Mate pet) =>
            pet.MateType != MateType.Pet
                ? []
                : catalog.GetSkills(pet.VNum)
                    .OrderBy(s => s.NpcMonsterSkillId)
                    .Select(s => _skillsByVnum.GetValueOrDefault(s.SkillVNum))
                    .OfType<SkillDto>()
                    .Where(s => s.IsMateSkill())
                    .Take(SkillBarSlots)
                    .ToList();

        public Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, int skillVNum, IAliveEntity target) =>
            owner.Mates.TryGetValue(mateTransportId, out var pet)
            && Skills(pet).FirstOrDefault(s => s.SkillVNum == skillVNum) is { } skill
                ? MateSkillCast.CastAsync(owner, pet, skill, target, battleService, distanceCalculator,
                    clock.GetCurrentInstant())
                : Task.FromResult(false);
    }
}
