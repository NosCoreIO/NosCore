//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class BlPacketHandler(ISessionRegistry sessionRegistry)
        : PacketHandler<BlPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BlPacket finsPacket, ClientSession session)
        {
            var target =
                sessionRegistry.GetCharacter(s => s.Name == finsPacket.CharacterName);

            if (target == null)
            {
                return session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
            }

            var blinsPacket = new BlInsPacket
            {
                CharacterId = target.VisualId
            };

            return session.HandlePacketsAsync(new[] { blinsPacket });
        }
    }
}
