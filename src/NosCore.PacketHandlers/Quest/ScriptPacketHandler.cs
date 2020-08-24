using System.Threading.Tasks;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.QuestProvider;
using NosCore.Packets.ClientPackets.Quest;

namespace NosCore.PacketHandlers.Quest
{
    public class ScriptPacketHandler : PacketHandler<ScriptClientPacket>, IWorldPacketHandler
    {
        private readonly IQuestProvider _questProvider;

        public ScriptPacketHandler(IQuestProvider questProvider)
        {
            _questProvider = questProvider;
        }

        public override Task ExecuteAsync(ScriptClientPacket scriptPacket, ClientSession session)
        {
            return _questProvider.RunScriptAsync(session.Character, scriptPacket);
        }
    }
}
