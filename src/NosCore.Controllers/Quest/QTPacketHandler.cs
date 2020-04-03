using System.Threading.Tasks;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.QuestProvider;
using NosCore.Packets.ClientPackets.Quest;

namespace NosCore.PacketHandlers.Quest
{
    public class QtPacketHandler : PacketHandler<QtPacket>, IWorldPacketHandler
    {
        private IQuestProvider _questProvider;

        public QtPacketHandler(IQuestProvider questProvider)
        {
            _questProvider = questProvider;
        }

        public override Task ExecuteAsync(QtPacket qtPacket, ClientSession session)
        {
            //switch (qtPacket.Type)
            //{
            //    // On Target Dest
            //    case 1:
            //        Session.Character.IncrementQuests(QuestType.GoTo, Session.CurrentMapInstance.Map.MapId, Session.Character.PositionX, Session.Character.PositionY);
            //        break;

            //    // Give Up Quest
            //    case 3:
            //        CharacterQuest charQuest = Session.Character.Quests?.FirstOrDefault(q => q.QuestNumber == qtPacket.Data);
            //        if (charQuest == null || charQuest.IsMainQuest)
            //        {
            //            return;
            //        }

            //        Session.Character.RemoveQuest(charQuest.QuestId, true);
            //        break;

            //    // Ask for rewards
            //    case 4:
            //        break;
            //}
            return Task.CompletedTask;
        }
    }
}
