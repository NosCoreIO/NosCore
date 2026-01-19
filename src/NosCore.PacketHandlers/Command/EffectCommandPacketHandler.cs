//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using System.Threading.Tasks;


namespace NosCore.PacketHandlers.Command
{
    public class EffectCommandPackettHandler : PacketHandler<EffectCommandPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(EffectCommandPacket effectCommandpacket, ClientSession session)
        {
            return session.Character.MapInstance.SendPacketAsync(
                session.Character.GenerateEff(effectCommandpacket.EffectId));
        }
    }
}
