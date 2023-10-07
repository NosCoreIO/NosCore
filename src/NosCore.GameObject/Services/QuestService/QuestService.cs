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

using Mapster;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.QuestService
{
    public class QuestService(List<ScriptDto> scripts,
            IOptions<WorldConfiguration> worldConfiguration, List<QuestDto> quests,
            List<QuestObjectiveDto> questObjectives, ILogger logger, IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : IQuestService
    {
        public Task RunScriptAsync(Character character) => RunScriptAsync(character, null);
        public async Task RunScriptAsync(Character character, ScriptClientPacket? packet)
        {
            if (character.CurrentScriptId == null) //todo handle other acts
            {
                if (worldConfiguration.Value.SceneOnCreate)
                {
                    await character.SendPacketAsync(new ScenePacket { SceneId = 40 }).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(71)).ConfigureAwait(false);
                }
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }

            if (packet != null)
            {
                if (!await CheckScriptStateAsync(packet, character).ConfigureAwait(false))
                {
                    return;
                }
                if (packet.Type == QuestActionType.Achieve)
                {
                    var quest = character.Quests.Values.FirstOrDefault(s => s.Quest.QuestId == packet.FirstArgument);
                    if (quest != null)
                    {
                        quest.CompletedOn = clock.GetCurrentInstant();
                        await character.SendPacketAsync(quest.GenerateQstiPacket(false)).ConfigureAwait(false);
                    }
                }
            }

            var orderedScripts = scripts.OrderBy(s => s.ScriptId).ThenBy(s => s.ScriptStepId).ToList();
            var nextScript = orderedScripts.FirstOrDefault(s => (s.ScriptId == (character.Script?.ScriptId ?? 0)) && (s.ScriptStepId > (character.Script?.ScriptStepId ?? 0))) ??
                orderedScripts.FirstOrDefault(s => s.ScriptId > (character.Script?.ScriptId ?? 0));
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
                case QuestActionType.Achieve:
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
            var script = scripts.FirstOrDefault(s => (s.ScriptId == scriptId) && (s.ScriptStepId == scriptStepId));
            if (script == null)
            {
                return false;
            }

            return script.StepType switch
            {
                "q_complete" => await ValidateQuestAsync(character, script.Argument1 ?? 0).ConfigureAwait(false),
                "quest" => await AddQuestAsync(character, type, script.Argument1 ?? 0).ConfigureAwait(false),
                "time" => await TimeAsync(script.Argument1 ?? 0).ConfigureAwait(false),
                "targetoff" => await TargetOffPacketAsync(script.Argument1 ?? 0, character).ConfigureAwait(false),
                "web" => true,
                "talk" => true,
                "openwin" => true,
                "opendual" => true,

                "move" => false, //todo handle
                "q_pay" => false, //todo handle
                "target" => false,  //todo handle
                "run" => true,  //todo handle
                _ => false,
            };
        }

        private async Task<bool> TimeAsync(short delay)
        {
            await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
            return true;
        }

        private async Task<bool> TargetOffPacketAsync(short questId, ICharacterEntity character)
        {
            var questDto = quests.FirstOrDefault(s => s.QuestId == questId);
            if (questDto != null)
            {
                return await ValidateQuestAsync(character, questId).ConfigureAwait(false);
            }

            logger.Error(logLanguage[LogLanguageKey.QUEST_NOT_FOUND]);
            return false;
        }

        public async Task<bool> AddQuestAsync(ICharacterEntity character, QuestActionType type, short questId)
        {
            var charQues = character.Quests.OrderByDescending(s => s.Value.CompletedOn).FirstOrDefault(s => s.Value.QuestId == questId);
            if (!charQues.Equals(new KeyValuePair<Guid, CharacterQuest>()) &&
                !charQues.Value.Quest.IsDaily)
            {
                return false;
            }

            var characterQuest =
                character.Quests.FirstOrDefault(s => s.Value.QuestId == questId && s.Value.CompletedOn == null);
            if (!characterQuest.Equals(new KeyValuePair<Guid, CharacterQuest>()))
            {
                var isValid = await ValidateQuestAsync(character, questId).ConfigureAwait(false);
                if (type != QuestActionType.Achieve)
                {
                    return isValid;
                }

                if (!isValid)
                {
                    return false;
                }

                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.QuestComplete
                }).ConfigureAwait(false);
                await character.SendPacketAsync(characterQuest.Value.GenerateQstiPacket(false)).ConfigureAwait(false);
                await character.SendPacketAsync(character.GenerateQuestPacket()).ConfigureAwait(false);
            }

            var questDto = quests.FirstOrDefault(s => s.QuestId == questId);
            if (questDto == null)
            {
                logger.Error(logLanguage[LogLanguageKey.QUEST_NOT_FOUND]);
                return true;
            }
            var quest = questDto.Adapt<Quest>();
            quest.QuestObjectives = questObjectives.Where(s => s.QuestId == questId).ToList();

            if (character.Quests.Where(s => s.Value.CompletedOn == null).Any(q => !q.Value.Quest.IsSecondary) ||
                (character.Quests.Where(s => s.Value.CompletedOn == null).Where(q => q.Value.Quest.QuestType != QuestType.WinRaid).ToList().Count >= 5 &&
                    quest.QuestType != QuestType.WinRaid))
            {
                return false;
            }

            if (quest.LevelMin > character.Level)
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.LevelTooLow
                }).ConfigureAwait(false);
                return false;
            }

            if (quest.LevelMax < character.Level)
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.LevelTooHigh
                }).ConfigureAwait(false);
                return false;
            }

            if (quest.IsDaily && (characterQuest.Value?.CompletedOn?.Plus(Duration.FromDays(1)) > clock.GetCurrentInstant()))
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.DailyQuestOncePerDay
                }).ConfigureAwait(false);
                return false;
            }

            character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                CharacterId = character.VisualId,
                Id = Guid.NewGuid(),
                Quest = quest,
                QuestId = quest.QuestId
            });
            await character.SendPacketAsync(character.GenerateQuestPacket()).ConfigureAwait(false);
            if (quest.TargetMap != null)
            {
                await character.SendPacketAsync(quest.GenerateTargetPacket()).ConfigureAwait(false);
            }
            return true;
        }

        public async Task<bool> ValidateQuestAsync(ICharacterEntity character, short questId)
        {
            var isValid = false;
            var characterQuest = character.Quests.Values.FirstOrDefault(s => s.QuestId == questId);
            switch (characterQuest?.Quest.QuestType)
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
                    isValid = (character.MapX <= (characterQuest.Quest.TargetX ?? 0) + 5 && character.MapX >= (characterQuest.Quest.TargetX ?? 0) - 5)
                        && (character.MapY <= (characterQuest.Quest.TargetY ?? 0) + 5 && character.MapY >= (characterQuest.Quest.TargetY ?? 0) - 5)
                        && (character.MapId == (characterQuest.Quest.TargetMap ?? 0));
                    if (isValid)
                    {
                        await character.SendPacketAsync(characterQuest.Quest.GenerateTargetOffPacket()).ConfigureAwait(false);
                    }
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