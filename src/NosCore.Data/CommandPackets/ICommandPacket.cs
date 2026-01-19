//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.Packets;
using NosCore.Packets.Interfaces;

namespace NosCore.Data.CommandPackets
{
    public interface ICommandPacket : IPacket
    {
        [UsedImplicitly]
        string Help();
    }

    public abstract class CommandPacket : PacketBase, ICommandPacket
    {
        public abstract string Help();
    }
}
