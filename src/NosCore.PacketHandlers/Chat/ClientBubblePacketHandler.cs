//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Chat;

namespace NosCore.PacketHandlers.Chat
{
    // The client announces its own bubble every few seconds; the map is shown the text the server accepted.
    public class ClientBubblePacketHandler(IClock clock) : PacketHandler<ClientBubblePacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(ClientBubblePacket packet, ClientSession session)
        {
            var character = session.Character;
            if (packet.CharacterId != character.VisualId
                || character.GenerateActiveBubble(clock.GetCurrentInstant()) is not { } bubble)
            {
                return Task.CompletedTask;
            }

            return character.MapInstance.SendPacketAsync(bubble);
        }
    }
}
