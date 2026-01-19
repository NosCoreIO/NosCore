//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;


namespace NosCore.PacketHandlers.Game
{
    public class TitEqPacketHandler : PacketHandler<TitEqPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(TitEqPacket titEqPacket, ClientSession session)
        {
            var tit = session.Character.Titles.FirstOrDefault(s => s.TitleType == titEqPacket.TitleId);
            if (tit == null)
            {
                return;
            }


            switch (titEqPacket.Mode)
            {
                case 1:
                    foreach (var title in session.Character.Titles.Where(s => s.TitleType != titEqPacket.TitleId))
                    {
                        title.Visible = false;
                    }
                    tit.Visible = !tit.Visible;
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.TitleChangedOrHidden
                    });
                    break;
                default:
                    foreach (var title in session.Character.Titles.Where(s => s.TitleType != titEqPacket.TitleId))
                    {
                        title.Active = false;
                    }
                    tit.Active = !tit.Active;
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.TitleEffectChangedOrDeactivated
                    });
                    break;
            }
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateTitInfo());
            await session.Character.SendPacketAsync(session.Character.GenerateTitle());
        }
    }
}
