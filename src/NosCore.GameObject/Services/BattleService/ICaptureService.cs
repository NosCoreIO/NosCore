//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService
{
    // Handles skills that carry a `BCardType.Capture` / `CaptureAnimal` bcard. Mirrors
    // vanosilla's BCardCaptureHandler + MonsterCaptureEventHandler flow (see
    // `_plugins/WingsEmu.Plugins.BasicImplementation/BCards/Handlers/BCardCaptureHandler.cs`).
    public interface ICaptureService
    {
        // True if `skill` is a capture skill (contains a CaptureAnimal bcard).
        // Callers use this to short-circuit the normal damage pipeline.
        bool IsCaptureSkill(SkillInfo skill);

        // Evaluates guards, rolls the capture rate, and on success inserts a Mate and
        // despawns the monster. No-op (and returns early) if `skill` is not a capture
        // skill or the target isn't a monster. Never throws for business-rule violations
        // — guard failures just skip the capture so the rest of the pipeline can react.
        Task TryCaptureAsync(IAliveEntity caster, IAliveEntity target, SkillInfo skill);
    }
}
