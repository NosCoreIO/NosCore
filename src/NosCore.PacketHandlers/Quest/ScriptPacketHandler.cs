using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Quest;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Quest
{
    public class ScriptPacketHandler : PacketHandler<ScriptClientPacket>, IWorldPacketHandler
    {
        private readonly IQuestService _questProvider;

        public ScriptPacketHandler(IQuestService questProvider)
        {
            _questProvider = questProvider;
        }

        public override Task ExecuteAsync(ScriptClientPacket scriptPacket, ClientSession session)
        {
            return _questProvider.RunScriptAsync(session.Character, scriptPacket);
        }
    }
}
