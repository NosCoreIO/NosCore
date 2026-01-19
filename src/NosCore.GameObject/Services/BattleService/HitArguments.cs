// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.BattleService;

public record HitArguments()
{
    public short? MapX { get; set; }
    public short? MapY { get; set; }
    public long SkillId { get; set; }
}
