//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.Packets.ClientPackets.Drops;
using System;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public interface IGetMapItemEventHandler : IEventHandler<MapItem, Tuple<MapItem, GetPacket>>
    {
    }
}
