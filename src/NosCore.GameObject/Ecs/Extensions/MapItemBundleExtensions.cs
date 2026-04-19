//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Extensions;

public static class MapItemBundleExtensions
{
    public static InPacket GenerateIn(this MapItemComponentBundle item)
    {
        return new InPacket
        {
            VisualType = VisualType.Object,
            VisualId = item.VisualId,
            VNum = item.VNum.ToString(),
            PositionX = item.PositionX,
            PositionY = item.PositionY,
            InItemSubPacket = new InItemSubPacket
            {
                Amount = item.Amount,
                IsQuestRelative = false,
                Owner = item.OwnerId ?? 0
            }
        };
    }

    public static DropPacket GenerateDrop(this MapItemComponentBundle item)
    {
        return new DropPacket
        {
            VNum = item.VNum,
            VisualId = item.VisualId,
            PositionX = item.PositionX,
            PositionY = item.PositionY,
            Amount = item.Amount,
            OwnerId = item.OwnerId
        };
    }
}
