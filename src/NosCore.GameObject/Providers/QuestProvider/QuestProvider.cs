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
using System.Threading.Tasks;
using Mapster;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
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
        private readonly List<ScriptDto> _scripts;
        private readonly List<QuestDto> _quests;
        private readonly List<QuestObjectiveDto> _questObjectives;
        private readonly WorldConfiguration _worldConfiguration;
        private readonly ILogger _logger;


        public QuestProvider(List<ScriptDto> scripts,
            WorldConfiguration worldConfiguration, List<QuestDto> quests, List<QuestObjectiveDto> questObjectives, ILogger logger)
        {
            _scripts = scripts;
            _quests = quests;
            _worldConfiguration = worldConfiguration;
            _logger = logger;
            _questObjectives = questObjectives;
        }

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

            if (packet != null && !await CheckScriptStateAsync(packet, character).ConfigureAwait(false))
            {
                return;
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
                case QuestActionType.Dialog:
                    scriptId = packet.FirstArgument;
                    scriptStepId = packet.SecondArgument;
                    break;
                case QuestActionType.Validate:
                    scriptId = packet.SecondArgument;
                    scriptStepId = packet.ThirdArgument;
                    break;
                default:
                    return false;
            }

            if (scriptId == null || scriptStepId == null)
            {
                return false;
            }

            return await IsValidScriptAsync(character, packet.Type, (int)scriptId, (int)scriptStepId).ConfigureAwait(false);
        }

        private async Task<bool> IsValidScriptAsync(ICharacterEntity character, QuestActionType type, int scriptId, int scriptStepId)
        {
            var script = _scripts.FirstOrDefault(s => (s.ScriptId == scriptId) && (s.ScriptStepId == scriptStepId));
            if (script == null)
            {
                return false;
            }
            return script.StepType switch
            {
                "q_complete" => ValidateQuest(character, script.Argument1 ?? 0),
                "quest" => (character.Script?.ScriptId != scriptId
                    || character.Script?.ScriptStepId != scriptStepId
                    || type != QuestActionType.Validate
                    || !script.Argument1.HasValue
                    || await AddQuestAsync(character, (short)script.Argument1).ConfigureAwait(false)
                ),
                "web" => true,
                "talk" => true,
                "target" => true,
                "openwin" => true,
                "opendual" => false,
                "time" => false,
                "move" => false,
                "run" => true,
                "q_pay" => true,
                "targetoff" => true,
                _ => false,
            };
        }

        public async Task<bool> AddQuestAsync(ICharacterEntity character, short questId)
        {
            var characterQuest = character.Quests.OrderByDescending(s => s.Value.CompletedOn).FirstOrDefault(s => s.Value.QuestId == questId);
            if (!characterQuest.Equals(new KeyValuePair<Guid, CharacterQuest>()) &&
                !characterQuest.Value.Quest.IsDaily)
            {
                return false;
            }


            var questDto = _quests.FirstOrDefault(s => s.QuestId == questId);
            if (questDto == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.QUEST_NOT_FOUND));
                return true;
            }
            var quest = questDto.Adapt<Quest>();
            quest.QuestObjectives = _questObjectives.Where(s => s.QuestId == questId).ToList();

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

            if (quest.IsDaily && (characterQuest.Value?.CompletedOn?.AddDays(1) > DateTime.Now))
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

            character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                CharacterId = character.VisualId,
                Id = Guid.NewGuid(),
                Quest = quest,
                QuestId = quest.QuestId
            });
            await character.SendPacketAsync(character.GenerateQuestPacket()).ConfigureAwait(false);

            return true;
        }

        public bool ValidateQuest(ICharacterEntity character, short questId)
        {
            bool isValid = false;
            var characterQuest = character.Quests.Values.First(s => s.QuestId == questId);
            switch (characterQuest.Quest.QuestType)
            {
                case QuestType.Hunt:
                    break;
                case QuestType.SpecialCollect:
                    break;
                case QuestType.CollectInRaid:
                    break;
                case QuestType.Brings:
                    break;
                case QuestType.CaptureWithoutGettingTheMonster:
                    break;
                case QuestType.Capture:
                    break;
                case QuestType.TimesSpace:
                    break;
                case QuestType.Product:
                    break;
                case QuestType.NumberOfKill:
                    break;
                case QuestType.TargetReput:
                    break;
                case QuestType.TsPoint:
                    break;
                case QuestType.Dialog1:
                    break;
                case QuestType.CollectInTs:
                    break;
                case QuestType.Required:
                    break;
                case QuestType.Wear:
                    break;
                case QuestType.Needed:
                    break;
                case QuestType.Collect:
                    break;
                case QuestType.TransmitGold:
                    break;
                case QuestType.GoTo:
                    isValid = (character.MapX == (characterQuest.Quest.TargetX ?? 0))
                        && (character.MapY == (characterQuest.Quest.TargetY ?? 0))
                        && (character.MapId == (characterQuest.Quest.TargetMap ?? 0));
                    break;
                case QuestType.CollectMapEntity:
                    break;
                case QuestType.Use:
                    break;
                case QuestType.Dialog2:
                    break;
                case QuestType.UnKnow:
                    break;
                case QuestType.Inspect:
                    break;
                case QuestType.WinRaid:
                    break;
                case QuestType.FlowerQuest:
                    break;
            }

            return isValid;
        }
    }
}