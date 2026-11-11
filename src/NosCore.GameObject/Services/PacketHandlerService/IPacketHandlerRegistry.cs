//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.Packets.Attributes;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.PacketHandlerService
{
    public interface IPacketHandlerRegistry
    {
        IPacketHandler? GetHandler(Type packetType);
        PacketHeaderAttribute? GetPacketAttribute(Type packetType);
        IReadOnlyList<RefusedWhileAttribute> GetRefusals(Type packetType);
    }
}
