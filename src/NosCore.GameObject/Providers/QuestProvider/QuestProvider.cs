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
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NosCore.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.Quest;

namespace NosCore.GameObject.Providers.QuestProvider
{
    public class QuestProvider : IQuestProvider
    {
        private readonly List<IEventHandler<QuestData, Tuple<CharacterQuestDto, QuestData>>>
            _handlers;

        private readonly List<ScriptDto> _scripts;
        private readonly WorldConfiguration _worldConfiguration;


        public QuestProvider(
            IEnumerable<IEventHandler<QuestData, Tuple<CharacterQuestDto, QuestData>>> handlers, List<ScriptDto> scripts, WorldConfiguration worldConfiguration)
        {
            _handlers = handlers.ToList();
            _scripts = scripts;
            _worldConfiguration = worldConfiguration;
        }

        public Task UpdateQuestAsync(ClientSession clientSession, QuestData data)
        {
            //search for quest with Objective Matching.
            var handlersRequest = new Subject<RequestData<Tuple<CharacterQuestDto, QuestData>>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    handlersRequest.Subscribe(async o =>
                    {
                        await Observable.FromAsync(async () =>
                        {
                            await handler.ExecuteAsync(o).ConfigureAwait(false);
                        });
                    });
                }
            });

            //foreach (var quest in clientSession.Character.Quests)
            //{
            //    handlersRequest.OnNext(new RequestData<Tuple<CharacterQuestDto, QuestData>>(clientSession,
            //        new Tuple<CharacterQuestDto, QuestData>(quest, data)));
            //    if (quest.AutoFinish)
            //    {
            //        ValidateQuest(clientSession, quest.Id);
            //    }
            //}

            return Task.CompletedTask;
        }

        public async Task CheckScriptAsync(ClientSession session)
        {
            if (session.Character.CurrentScriptId == null)
            {
                if (_worldConfiguration.SceneOnCreate)
                {
                    await session.SendPacketAsync(new ScenePacket { SceneId = 40 }).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(75)).ConfigureAwait(false);
                }
            }

            var firstScript = _scripts.OrderBy(s => s.ScriptId).ThenBy(s => s.ScriptStepId).FirstOrDefault();
            if (firstScript == null)
            {
                return;
            }
            session.Character.CurrentScriptId = firstScript.Id;
            session.Character.Script = firstScript;
            await session.SendPacketAsync(new ScriptPacket()
            {
                ScriptId = firstScript.ScriptId,
                ScriptStepId = firstScript.ScriptStepId
            }).ConfigureAwait(false);
        }

        public Task ValidateQuest(ClientSession clientSession, Guid characterQuestId)
        {
            return Task.CompletedTask;
            //get quest
            //check all valid
            //ApplyBonus
            //delete objectives
            //delete quest
        }
    }
}