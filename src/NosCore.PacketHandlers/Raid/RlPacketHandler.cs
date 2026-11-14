//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RaidService;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Raid
{
    public class RlPacketHandler(IRaidService raidService) : PacketHandler<RlPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RlPacket packet, ClientSession session)
        {
            return (RlPacketType)packet.Type switch
            {
                RlPacketType.Normal => raidService.ShowBoardAsync(session),
                RlPacketType.WantToRegister => raidService.ListAsync(session),
                RlPacketType.WantToUnregister => raidService.UnlistAsync(session),
                RlPacketType.MemberAndAlreadyInList when packet.CharacterName != null =>
                    raidService.JoinListedAsync(session, packet.CharacterName),
                _ => Task.CompletedTask
            };
        }
    }
}
