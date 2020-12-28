using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Quest
{
    public class QtPacketHandler : PacketHandler<QtPacket>, IWorldPacketHandler
    {
        private readonly IQuestService _questProvider;

        public QtPacketHandler(IQuestService questProvider)
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
                case QuestActionType.Validate:
                    await _questProvider.RunScriptAsync(session.Character, session.Character.Script == null ? null : new ScriptClientPacket
                    {
                        Type = QuestActionType.Validate,
                        FirstArgument = session.Character.Script.Argument1,
                        SecondArgument = session.Character.Script.ScriptId,
                        ThirdArgument = session.Character.Script.ScriptStepId,
                    }).ConfigureAwait(false);
                    break;

                case QuestActionType.Achieve:
                    await _questProvider.RunScriptAsync(session.Character, session.Character.Script == null ? null : new ScriptClientPacket
                    {
                        Type = QuestActionType.Achieve,
                        FirstArgument = session.Character.Script.Argument1,
                        SecondArgument = session.Character.Script.ScriptId,
                        ThirdArgument = session.Character.Script.ScriptStepId,
                    }).ConfigureAwait(false);
                    break;

                case QuestActionType.GiveUp:
                    session.Character.Quests.TryRemove(charQuest.Key, out var questToRemove);
                    questToRemove?.GenerateQstiPacket(false);
                    break;
            }
        }
    }
}
