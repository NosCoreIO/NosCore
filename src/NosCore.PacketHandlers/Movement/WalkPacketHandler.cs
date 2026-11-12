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
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Movement
{
    public class WalkPacketHandler(IMovementService movementService)
        : PacketHandler<WalkPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(WalkPacket walkPacket, ClientSession session)
        {
            var result = await movementService.WalkAsync(session, walkPacket.XCoordinate, walkPacket.YCoordinate,
                walkPacket.Speed, walkPacket.CheckSum);
            switch (result)
            {
                case WalkResult.Refused:
                    await session.SendPacketAsync(session.Character.GenerateTp());
                    break;
                case WalkResult.Rejected:
                    await session.DisconnectAsync();
                    break;
            }
        }
    }
}
