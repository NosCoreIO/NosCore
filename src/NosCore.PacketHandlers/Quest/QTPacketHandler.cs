//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.GameObject;
using NosCore.GameObject.Networking;
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
            var charQuest = session.Player.Quests.FirstOrDefault(q => q.Value.QuestId == qtPacket.Data);
            if (charQuest.Equals(new KeyValuePair<Guid, CharacterQuest>()))
            {
                return;
            }

            switch (qtPacket.Type)
            {
                case QuestActionType.Validate:
                    await questProvider.RunScriptAsync(session.Player, session.Player.Script == null ? null : new ScriptClientPacket
                    {
                        Type = QuestActionType.Validate,
                        FirstArgument = session.Player.Script.Argument1,
                        SecondArgument = session.Player.Script.ScriptId,
                        ThirdArgument = session.Player.Script.ScriptStepId,
                    }).ConfigureAwait(false);
                    break;

                case QuestActionType.Achieve:
                    await questProvider.RunScriptAsync(session.Player, session.Player.Script == null ? null : new ScriptClientPacket
                    {
                        Type = QuestActionType.Achieve,
                        FirstArgument = session.Player.Script.Argument1,
                        SecondArgument = session.Player.Script.ScriptId,
                        ThirdArgument = session.Player.Script.ScriptStepId,
                    }).ConfigureAwait(false);
                    break;

                case QuestActionType.GiveUp:
                    session.Player.Quests.TryRemove(charQuest.Key, out var questToRemove);
                    questToRemove?.GenerateQstiPacket(false);
                    break;
            }
        }
    }
}
