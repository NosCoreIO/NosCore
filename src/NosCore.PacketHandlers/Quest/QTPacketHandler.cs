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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Quest
{
    public class QtPacketHandler(IQuestService questProvider) : PacketHandler<QtPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(QtPacket qtPacket, ClientSession session)
        {
            switch (qtPacket.Type)
            {
                case QuestActionType.Validate:
                case QuestActionType.Achieve:
                    if (session.Character.Script == null)
                    {
                        return;
                    }
                    await questProvider.RunScriptAsync(session.Character, new ScriptClientPacket
                    {
                        Type = qtPacket.Type,
                        FirstArgument = session.Character.Script.Argument1,
                        SecondArgument = session.Character.Script.ScriptId,
                        ThirdArgument = session.Character.Script.ScriptStepId,
                    });
                    break;

                case QuestActionType.GiveUp:
                    var charQuest = session.Character.Quests.FirstOrDefault(q => q.Value.QuestId == qtPacket.Data);
                    if (charQuest.Equals(new KeyValuePair<Guid, CharacterQuest>()))
                    {
                        return;
                    }
                    session.Character.Quests.TryRemove(charQuest.Key, out var questToRemove);
                    questToRemove?.GenerateQstiPacket(false);
                    break;
            }
        }
    }
}
