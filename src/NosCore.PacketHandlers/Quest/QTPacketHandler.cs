//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Quest
{
    public class QtPacketHandler(IQuestService questProvider) : PacketHandler<QtPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(QtPacket qtPacket, ClientSession session)
        {
            return qtPacket.Type switch
            {
                QuestActionType.Validate or QuestActionType.Achieve =>
                    questProvider.RunCurrentScriptStepAsync(session.Character, qtPacket.Type),
                QuestActionType.GiveUp => questProvider.GiveUpQuestAsync(session.Character, qtPacket.Data),
                _ => Task.CompletedTask,
            };
        }
    }
}
