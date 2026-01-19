//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Infastructure
{
    public interface IPacketHandler
    {
        Task ExecuteAsync(IPacket packet, ClientSession clientSession);
    }

    public interface ILoginPacketHandler
    {
    }

    public interface IWorldPacketHandler
    {
    }

    public interface IPacketHandler<in TPacket> : IPacketHandler where TPacket : IPacket
    {
        Task ExecuteAsync(TPacket packet, ClientSession clientSession);
    }

    public abstract class PacketHandler<TPacket> : IPacketHandler<TPacket> where TPacket : IPacket
    {
        public abstract Task ExecuteAsync(TPacket packet, ClientSession clientSession);

        public Task ExecuteAsync(IPacket packet, ClientSession clientSession)
        {
            return ExecuteAsync((TPacket)packet, clientSession);
        }
    }
}
