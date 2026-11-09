//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;


namespace NosCore.PacketHandlers.Game
{
    public class TitEqPacketHandler : PacketHandler<TitEqPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(TitEqPacket titEqPacket, ClientSession session)
        {
            var visibility = titEqPacket.Mode == 1;
            var toggled = visibility
                ? session.Character.ToggleTitleVisibility(titEqPacket.TitleId)
                : session.Character.ToggleTitleEffect(titEqPacket.TitleId);
            if (!toggled)
            {
                return;
            }

            await session.SendPacketAsync(new InfoiPacket
            {
                Message = visibility
                    ? Game18NConstString.TitleChangedOrHidden
                    : Game18NConstString.TitleEffectChangedOrDeactivated
            });
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateTitInfo());
            await session.Character.SendPacketAsync(session.Character.GenerateTitle());
        }
    }
}
