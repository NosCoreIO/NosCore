//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService
{
    public interface INrunService
    {
        Task NRunLaunchAsync(ClientSession clientSession, Tuple<IAliveEntity, NrunPacket> data);
    }
}
