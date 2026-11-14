//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class PetSkillService(IClock clock, IPetSkillBarService petSkillBarService, IBattleService battleService,
        IHeuristic distanceCalculator) : IPetSkillService, ISingletonService
    {
        public async Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, short skillVNum,
            IAliveEntity target)
        {
            if (!owner.Mates.TryGetValue(mateTransportId, out var pet))
            {
                return false;
            }

            var bar = petSkillBarService.Skills(pet);
            var slot = bar.Select(s => s.SkillVNum).ToList().IndexOf(skillVNum);
            if (slot < 0
                || !await MateSkillCast.CastAsync(owner, pet, bar[slot], target, battleService, distanceCalculator,
                    clock.GetCurrentInstant()))
            {
                return false;
            }

            // The map tells the owner when the slot is ready again, as it does for a player's own skills.
            if (pet.Entity is { } entity && entity.NextUsableAt.TryGetValue(skillVNum, out var readyAt))
            {
                entity.PendingResets[slot] = readyAt;
            }

            return true;
        }
    }
}
