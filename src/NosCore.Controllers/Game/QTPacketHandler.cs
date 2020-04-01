using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Quest;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.GuriProvider;
using NosCore.GameObject.Providers.QuestProvider;
using NosCore.Packets.ClientPackets.Quest;

namespace NosCore.PacketHandlers.Game
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
