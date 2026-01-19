//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.Packets.Attributes;
using System;

namespace NosCore.GameObject.Services.PacketHandlerService
{
    public interface IPacketHandlerRegistry
    {
        IPacketHandler? GetHandler(Type packetType);
        PacketHeaderAttribute? GetPacketAttribute(Type packetType);
    }
}
