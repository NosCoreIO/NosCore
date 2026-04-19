//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs;

public static class DignityLevels
{
    public static DignityType FromDignity(short dignity) => dignity switch
    {
        <= -801 => DignityType.Failed,
        <= -601 => DignityType.Useless,
        <= -401 => DignityType.Unqualified,
        <= -201 => DignityType.Dreadful,
        <= -100 => DignityType.Dubious,
        _ => DignityType.Default
    };
}
