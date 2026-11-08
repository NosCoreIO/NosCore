//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Networking.ClientSession;

public enum PacketRateDecision
{
    Allow,
    Drop,
    Disconnect
}

public interface IPacketRateLimitService
{
    PacketRateDecision Check(ClientSession session, IPacket packet);
}
