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

using System.Collections.Generic;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Quest;

namespace NosCore.GameObject.Services.QuestService
{
    public class CharacterQuest : CharacterQuestDto
    {
        public Quest Quest { get; set; } = null!;

        public QstiPacket GenerateQstiPacket(bool showDialog)
        {
            return new QstiPacket
            {
                QuestSubPacket = GenerateQuestSubPacket(showDialog)
            };
        }

        public QuestSubPacket GenerateQuestSubPacket(bool showDialog)
        {
            var objectives = new List<QuestObjectiveSubPacket>();
            var questCount = 0;
            foreach (var objective in Quest.QuestObjectives)
            {
                //todo add objective
                objectives.Add(new QuestObjectiveSubPacket()
                {
                    CurrentCount = 0,
                    MaxCount = 5,
                    IsFinished = questCount == 0 ? CompletedOn != null : (bool?)null
                });
                questCount++;
            }

            return new QuestSubPacket
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
        public TargetPacket GenerateTargetPacket()
        {
            return new TargetPacket
            {
                QuestId = QuestId, TargetMap = TargetMap ?? 0, TargetX = TargetX ?? 0,
                TargetY = TargetY ?? 0
            };
        }

        public TargetOffPacket GenerateTargetOffPacket()
        {
            return new TargetOffPacket
            {
                QuestId = QuestId, TargetMap = TargetMap ?? 0, TargetX = TargetX ?? 0,
                TargetY = TargetY ?? 0
            };
        }

        public List<QuestObjectiveDto> QuestObjectives { get; set; } = null!;
    }
}