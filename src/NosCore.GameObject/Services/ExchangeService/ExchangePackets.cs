//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.Packets.ServerPackets.Exchanges;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ExchangeService;

public static class ExchangePackets
{
    public static ServerExcListPacket GenerateServerExcListPacket(this PlayerComponentBundle aliveEntity, long? gold,
        long? bankGold, List<ServerExcListSubPacket?>? subPackets)
    {
        return new ServerExcListPacket
        {
            VisualType = aliveEntity.VisualType,
            VisualId = aliveEntity.VisualId,
            Gold = gold,
            SubPackets = subPackets,
            BankGold = bankGold
        };
    }
}
