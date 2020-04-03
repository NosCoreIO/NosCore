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

using System;
using System.Collections.Generic;
using NosCore.Data.Dto;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Quest;

namespace NosCore.GameObject
{
    public class CharacterQuest : CharacterQuestDto
    {
        public new Quest Quest { get; set; } = null!;

        public QstiPacket GenerateQstiPacket(bool showDialog)
        {
            var objectives = new List<QuestObjectiveSubPacket>();
            var questCount = 0;
            foreach (var objective in Quest.QuestObjectives)
            {
                objectives.Add(new QuestObjectiveSubPacket()
                {
                    CurrentCount = 0,
                    MaxCount = 5,
                    IsFinished = questCount == 0 ? false : (bool?)null
                });
                questCount++;
            }
            return new QstiPacket
            {
                QuestId = QuestId,
                InfoId = QuestId,
                GoalType = Quest.QuestType,
                ObjectiveCount = 5,
                ShowDialog = showDialog,
                QuestObjectiveSubPackets = objectives
            };
        }
    }

    public class Quest : QuestDto
    {
        public List<QuestObjectiveDto> QuestObjectives { get; set; } = null!;
    }
}