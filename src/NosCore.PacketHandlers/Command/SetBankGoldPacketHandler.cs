//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Command
{
    public class SetBankGoldPacketHandler : PacketHandler<SetBankGoldPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SetBankGoldPacket packet, ClientSession session)
        {
            session.Character.BankGold = packet.BankGold < 0 ? 0 : packet.BankGold;
            return Task.CompletedTask;
        }
    }
}
