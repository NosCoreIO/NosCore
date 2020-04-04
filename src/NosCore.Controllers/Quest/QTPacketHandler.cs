using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.QuestProvider;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;

namespace NosCore.PacketHandlers.Quest
{
    public class QtPacketHandler : PacketHandler<QtPacket>, IWorldPacketHandler
    {
        private readonly IQuestProvider _questProvider;

        public QtPacketHandler(IQuestProvider questProvider)
        {
            _questProvider = questProvider;
        }

        public override async Task ExecuteAsync(QtPacket qtPacket, ClientSession session)
        {
            var charQuest = session.Character.Quests.FirstOrDefault(q => q.Value.QuestId == qtPacket.Data);
            if (charQuest.Equals(new KeyValuePair<Guid, CharacterQuest>()))
            {
                return;
            }

            switch (qtPacket.Type)
            {
                case QuestActionType.Achieve:
                    await _questProvider.RunScriptAsync(session.Character).ConfigureAwait(false);
                    break;

                case QuestActionType.GiveUp:

                    session.Character.Quests.TryRemove(charQuest.Key, out _);
                    break;

                case QuestActionType.Validate:
                    break;
            }
        }
    }
}
