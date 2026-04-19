//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.Packets.ClientPackets.Drops;
using System;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public interface IGetMapItemEventHandler : IEventHandler<MapItemComponentBundle, Tuple<MapItemComponentBundle, GetPacket>>
    {
    }
}
