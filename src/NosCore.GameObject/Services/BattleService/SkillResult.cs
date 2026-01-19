// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.BattleService;

public record SkillResult
{
    public int SkillVnum { get; set; }
    public int SkillCooldown { get; set; }
    public int AttackAnimation { get; set; }
    public int SkillEffect { get; set; }
    public int SkillTypeMinusOne { get; set; }
    public long CastId { get; set; }
}
