//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

public interface IFalconService
{
    // The falcon a skill sent after an entity attacks it from the air, wherever it stands on the map.
    Task<bool> StrikeAsync(PlayerComponentBundle player);
}

public sealed class FalconService(IBattleService battleService, ISkillCatalog skills, ISkillCastService skillCastService,
    ISessionRegistry sessionRegistry, IClock clock) : IFalconService, ISingletonService
{
    // The Hunting Falcon's own attack.
    private const short HuntingFalconAttackVNum = 1249;

    public async Task<bool> StrikeAsync(PlayerComponentBundle player)
    {
        if (player.FalconTargetId == 0 || !player.CanFight || player.MapInstance is not { } map
            || clock.GetCurrentInstant() < player.FalconReadyAt
            || skills.GetSkill(HuntingFalconAttackVNum) is not { } attack)
        {
            return false;
        }

        if (Find(map, player.FalconTargetType, player.FalconTargetId) is not { IsAlive: true } target
            || !ReferenceEquals(target.MapInstance, map))
        {
            player.CallFalconBack();
            return false;
        }

        if (!await battleService.Hit(player, target, new HitArguments { SkillId = attack.CastId, Skill = attack }))
        {
            return false;
        }

        player.FalconReadyAt = clock.GetCurrentInstant().Plus(skillCastService.Cooldown(player, attack.Cooldown));
        return true;
    }

    private IAliveEntity? Find(MapInstance map, VisualType visualType, long visualId) => visualType switch
    {
        VisualType.Player => sessionRegistry.TryGetCharacter(s => s.VisualId == visualId, out var player) ? player : null,
        VisualType.Npc => map.FindNpc(s => s.VisualId == visualId),
        VisualType.Monster => map.FindMonster(s => s.VisualId == visualId),
        _ => null,
    };
}
