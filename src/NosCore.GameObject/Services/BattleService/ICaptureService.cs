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
    // Handles skills that carry a `BCardType.Capture` / `CaptureAnimal` bcard.
    public interface ICaptureService
    {
        // True if `skill` is a capture skill (contains a CaptureAnimal bcard).
        // Callers use this to short-circuit the normal damage pipeline.
        bool IsCaptureSkill(SkillInfo skill);

        // Evaluates guards, rolls the capture rate, and on success inserts a Mate and
        // despawns the monster. False when the target cannot be captured at all (not a
        // monster, out of reach): the attempt then costs nothing. A refusal for the
        // monster's level or health is still an attempt and returns true.
        Task<bool> TryCaptureAsync(IAliveEntity caster, IAliveEntity target, SkillInfo skill);
    }
}
