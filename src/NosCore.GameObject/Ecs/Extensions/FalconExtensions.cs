//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Ecs.Extensions;

public static class FalconExtensions
{
    public static void SendFalconAfter(this PlayerComponentBundle player, IAliveEntity target)
    {
        player.FalconTargetType = target.VisualType;
        player.FalconTargetId = target.VisualId;
    }

    public static void CallFalconBack(this PlayerComponentBundle player) => player.FalconTargetId = 0;
}
