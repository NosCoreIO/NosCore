//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class FlCommandPacketHandler(ISessionRegistry sessionRegistry)
        : PacketHandler<FlCommandPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FlCommandPacket flPacket, ClientSession session)
        {
            var target =
                sessionRegistry.GetCharacter(s => s.Name == flPacket.CharacterName);

            if (target == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
                return;
            }

            var fins = new FinsPacket
            {
                CharacterId = target.VisualId,
                Type = FinsPacketType.Accepted
            };

            await session.HandlePacketsAsync(new[] { fins });
        }
    }
}
