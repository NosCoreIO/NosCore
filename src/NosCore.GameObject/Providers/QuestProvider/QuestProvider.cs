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
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Quest;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.UI;
using Serilog;

namespace NosCore.GameObject.Providers.QuestProvider
{
    public class QuestProvider : IQuestProvider
    {
        private readonly List<IEventHandler<QuestData, Tuple<CharacterQuestDto, QuestData>>>
            _handlers;

        private readonly List<ScriptDto> _scripts;
        private readonly List<QuestDto> _quests;
        private readonly WorldConfiguration _worldConfiguration;
        private readonly ILogger _logger;


        public QuestProvider(
            IEnumerable<IEventHandler<QuestData, Tuple<CharacterQuestDto, QuestData>>> handlers, List<ScriptDto> scripts,
            WorldConfiguration worldConfiguration, List<QuestDto> quests, ILogger logger)
        {
            _handlers = handlers.ToList();
            _scripts = scripts;
            _quests = quests;
            _worldConfiguration = worldConfiguration;
            _logger = logger;
        }

        //public Task UpdateQuestAsync(ClientSession clientSession, QuestData data)
        //{
        //    //search for quest with Objective Matching.
        //    var handlersRequest = new Subject<RequestData<Tuple<CharacterQuestDto, QuestData>>>();
        //    _handlers.ForEach(handler =>
        //    {
        //        if (handler.Condition(data))
        //        {
        //            handlersRequest.Subscribe(async o =>
        //            {
        //                await Observable.FromAsync(async () =>
        //                {
        //                    await handler.ExecuteAsync(o).ConfigureAwait(false);
        //                });
        //            });
        //        }
        //    });

        //    //foreach (var quest in clientSession.Character.Quests)
        //    //{
        //    //    handlersRequest.OnNext(new RequestData<Tuple<CharacterQuestDto, QuestData>>(clientSession,
        //    //        new Tuple<CharacterQuestDto, QuestData>(quest, data)));
        //    //    if (quest.AutoFinish)
        //    //    {
        //    //        ValidateQuest(clientSession, quest.Id);
        //    //    }
        //    //}

        //    return Task.CompletedTask;
        //}

        public Task RunScriptAsync(ICharacterEntity character) => RunScriptAsync(character, null);
        public async Task RunScriptAsync(ICharacterEntity character, ScriptClientPacket? packet)
        {
            if (character.CurrentScriptId == null)
            {
                if (_worldConfiguration.SceneOnCreate)
                {
                    await character.SendPacketAsync(new ScenePacket { SceneId = 40 }).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(71)).ConfigureAwait(false);
                }
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }

            if (packet != null)
            {
                if (!await CheckScriptStateAsync(packet, character).ConfigureAwait(false))
                {
                    return;
                }
                //todo validate script complete
            }

            var scripts = _scripts.OrderBy(s => s.ScriptId).ThenBy(s => s.ScriptStepId).ToList();
            var nextScript = scripts.FirstOrDefault(s => (s.ScriptId == (character.Script?.ScriptId ?? 0)) && (s.ScriptStepId > (character.Script?.ScriptStepId ?? 0))) ??
                scripts.FirstOrDefault(s => s.ScriptId > (character.Script?.ScriptId ?? 0));
            if (nextScript == null)
            {
                return;
            }
            character.CurrentScriptId = nextScript.Id;
            character.Script = nextScript;
            await character.SendPacketAsync(new ScriptPacket()
            {
                ScriptId = nextScript.ScriptId,
                ScriptStepId = nextScript.ScriptStepId
            }).ConfigureAwait(false);
        }

        private async Task<bool> CheckScriptStateAsync(ScriptClientPacket packet, ICharacterEntity character)
        {
            int? scriptId;
            int? scriptStepId;
            switch (packet.Type)
            {
                case ScriptType.Dialog:
                    scriptId = packet.FirstArgument;
                    scriptStepId = packet.SecondArgument;
                    break;
                case ScriptType.Quest:
                    scriptId = packet.SecondArgument;
                    scriptStepId = packet.ThirdArgument;
                    break;
                default:
                    return false;
            }

            if (character.Script?.ScriptId != scriptId
                || character.Script?.ScriptStepId != scriptStepId
                || packet.Type != ScriptType.Quest
                || !packet.FirstArgument.HasValue
                || await AddQuestAsync(character, (short)packet.FirstArgument).ConfigureAwait(false)
                )
            {
                return false;
            }

            if (true) // check quest is completed
            {
                return true;
            }
        }

        public async Task<bool> AddQuestAsync(ICharacterEntity character, short questId)
        {
            var characterQuest = character.Quests.OrderByDescending(s => s.Value.CompletedOn).FirstOrDefault(s => s.Value.QuestId == questId);
            if (!characterQuest.Equals(new KeyValuePair<Guid, CharacterQuestDto>()) &&
                !characterQuest.Value.Quest.IsDaily)
            {
                return false;
            }

            {
                var quest = _quests.FirstOrDefault(s => s.QuestId == questId);
                if (quest == null)
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.QUEST_NOT_FOUND));
                    return true;
                }

                if (character.Quests.Any(q => !q.Value.Quest.IsSecondary) ||
                    (character.Quests.Where(q => q.Value.Quest.QuestType != QuestType.WinRaid).ToList().Count >= 5 &&
                        quest.QuestType != QuestType.WinRaid))
                {
                    return false;
                }

                if (quest.LevelMin > character.Level)
                {
                    await character.SendPacketAsync(new MsgPacket
                    {
                        Type = MessageType.Whisper,
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL, character.AccountLanguage)
                    }).ConfigureAwait(false);
                    return false;
                }

                if (quest.LevelMax < character.Level)
                {
                    await character.SendPacketAsync(new MsgPacket
                    {
                        Type = MessageType.Whisper,
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.TOO_HIGH_LEVEL, character.AccountLanguage)
                    }).ConfigureAwait(false);
                    return false;
                }

                if (characterQuest.Value.Quest.IsDaily && (characterQuest.Value.CompletedOn?.AddDays(1) > DateTime.Now))
                {
                    await character.SendPacketAsync(new MsgPacket
                    {
                        Type = MessageType.Whisper,
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.QUEST_ALREADY_DONE, character.AccountLanguage)
                    }).ConfigureAwait(false);
                    return false;
                }

                if (quest.TargetMap == character.MapId)
                {
                    await character.SendPacketAsync(new TargetPacket
                    {
                        QuestId = quest.QuestId, TargetMap = quest.TargetMap ?? 0, TargetX = quest.TargetX ?? 0,
                        TargetY = quest.TargetY ?? 0
                    }).ConfigureAwait(false);
                }

                character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuestDto
                {
                    CharacterId = character.VisualId,
                    Id = Guid.NewGuid(),
                    Quest = quest,
                    QuestId = quest.QuestId
                });
                //await character.SendPacketAsync(character.GenerateQuestPacket()).ConfigureAwait(false);

                return true;
            }

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