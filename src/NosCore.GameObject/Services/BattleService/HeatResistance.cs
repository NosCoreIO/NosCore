//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;

namespace NosCore.GameObject.Services.BattleService;

public static class HeatResistance
{
    public const short CardId = 340;
    public const short BurningCardId = 339;
    public static readonly Duration MaximumDuration = Duration.FromHours(2);
}
