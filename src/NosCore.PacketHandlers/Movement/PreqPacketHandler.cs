//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MovementService;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Movement
{
    public class PreqPacketHandler(IMovementService movementService)
        : PacketHandler<PreqPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PreqPacket _, ClientSession session)
        {
            if (await movementService.UsePortalAsync(session) != PortalResult.TooSoon)
            {
                return;
            }

            await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.WillMoveShortly));
        }
    }
}
