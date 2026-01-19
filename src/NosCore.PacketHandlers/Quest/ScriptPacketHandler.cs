//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Quest;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Quest
{
    public class ScriptPacketHandler(IQuestService questProvider) : PacketHandler<ScriptClientPacket>,
        IWorldPacketHandler
    {
        public override Task ExecuteAsync(ScriptClientPacket scriptPacket, ClientSession session)
        {
            return questProvider.RunScriptAsync(session.Character, scriptPacket);
        }
    }
}
