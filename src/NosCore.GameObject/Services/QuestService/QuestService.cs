//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.QuestService
{
    public class QuestService(List<ScriptDto> scripts,
            IOptions<WorldConfiguration> worldConfiguration, List<QuestDto> quests,
            List<QuestObjectiveDto> questObjectives, ILogger logger, IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IEnumerable<IQuestTypeHandler> questTypeHandlers,
            Wolverine.IMessageBus messageBus,
            List<QuestRewardDto> questRewards,
            List<QuestQuestRewardDto> questQuestRewards,
            Services.ItemGenerationService.IItemGenerationService itemBuilderService)
        : IQuestService
    {
        public Task RunScriptAsync(ICharacterEntity character) => RunScriptAsync(character, null);
        public async Task RunScriptAsync(ICharacterEntity character, ScriptClientPacket? packet)
        {
            if (character.CurrentScriptId == null) //todo handle other acts
            {
                if (worldConfiguration.Value.SceneOnCreate)
                {
                    await character.SendPacketAsync(new ScenePacket { SceneId = 40 });
                    await Task.Delay(TimeSpan.FromSeconds(71));
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            if (packet != null)
            {
                if (!await CheckScriptStateAsync(packet, character))
                {
                    return;
                }
                if (packet.Type == QuestActionType.Achieve)
                {
                    var quest = character.Quests.Values.FirstOrDefault(s => s.Quest.QuestId == packet.FirstArgument);
                    if (quest != null)
                    {
                        quest.CompletedOn = clock.GetCurrentInstant();
                        await character.SendPacketAsync(quest.GenerateQstiPacket(false));
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
            character.Script = nextScript;
            await character.SendPacketAsync(new ScriptPacket()
            {
                ScriptId = nextScript.ScriptId,
                ScriptStepId = nextScript.ScriptStepId
            });
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

            return await IsValidScriptAsync(character, packet.Type, (int)scriptId, (int)scriptStepId);
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
                "q_complete" => await ValidateQuestAsync(character, script.Argument1 ?? 0),
                "quest" => await AddQuestAsync(character, type, script.Argument1 ?? 0),
                "q_pay" => await QPayAsync(character, script.Argument1 ?? 0),
                "time" => await TimeAsync(script.Argument1 ?? 0),
                "targetoff" => await TargetOffPacketAsync(script.Argument1 ?? 0, character),
                "web" => true,
                "talk" => true,
                "openwin" => true,
                "opendual" => true,

                "move" => false, //todo handle
                "target" => false,  //todo handle
                "run" => true,  //todo handle
                _ => false,
            };
        }

        private async Task<bool> QPayAsync(ICharacterEntity character, short questId)
        {
            var charQuest = character.Quests.Values.FirstOrDefault(q => q.QuestId == questId && q.CompletedOn == null);
            if (charQuest == null || !charQuest.AreObjectivesComplete())
            {
                return false;
            }

            foreach (var link in questQuestRewards.Where(l => l.QuestId == questId))
            {
                var reward = questRewards.FirstOrDefault(r => r.QuestRewardId == link.QuestRewardId);
                if (reward != null)
                {
                    await ApplyRewardAsync(character, reward);
                }
            }

            charQuest.CompletedOn = clock.GetCurrentInstant();
            await character.SendPacketAsync(character.GenerateQuestPacket());
            await messageBus.PublishAsync(new QuestCompletedEvent(character, charQuest));
            return true;
        }

        private async Task ApplyRewardAsync(ICharacterEntity character, QuestRewardDto reward)
        {
            var amount = Math.Max(1, reward.Amount);
            switch ((Data.Enumerations.Quest.QuestRewardType)reward.RewardType)
            {
                case Data.Enumerations.Quest.QuestRewardType.Gold:
                case Data.Enumerations.Quest.QuestRewardType.BaseGoldByAmount:
                case Data.Enumerations.Quest.QuestRewardType.CapturedGold:
                case Data.Enumerations.Quest.QuestRewardType.UnknowGold:
                    character.Gold += (long)reward.Data * amount;
                    break;
                case Data.Enumerations.Quest.QuestRewardType.Exp:
                case Data.Enumerations.Quest.QuestRewardType.PercentExp:
                    character.LevelXp += (long)reward.Data * amount;
                    break;
                case Data.Enumerations.Quest.QuestRewardType.JobExp:
                case Data.Enumerations.Quest.QuestRewardType.PercentJobExp:
                    character.JobLevelXp += (long)reward.Data * amount;
                    break;
                case Data.Enumerations.Quest.QuestRewardType.Reput:
                    character.Reput += (long)reward.Data * amount;
                    break;
                case Data.Enumerations.Quest.QuestRewardType.EtcMainItem:
                case Data.Enumerations.Quest.QuestRewardType.WearItem:
                    var item = itemBuilderService.Create((short)reward.Data, (short)amount, (sbyte)reward.Rarity, reward.Upgrade, reward.Design);
                    var added = character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, character.VisualId));
                    if (added != null)
                    {
                        foreach (var inv in added)
                        {
                            await character.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));
                        }
                    }
                    break;
                default:
                    logger.Warning("Unhandled quest reward type {Type}", reward.RewardType);
                    break;
            }
        }

        private async Task<bool> TimeAsync(short delay)
        {
            await Task.Delay(TimeSpan.FromSeconds(delay));
            return true;
        }

        private async Task<bool> TargetOffPacketAsync(short questId, ICharacterEntity character)
        {
            var questDto = quests.FirstOrDefault(s => s.QuestId == questId);
            if (questDto != null)
            {
                return await ValidateQuestAsync(character, questId);
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
                var isValid = await ValidateQuestAsync(character, questId);
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
                });
                await character.SendPacketAsync(characterQuest.Value.GenerateQstiPacket(false));
                await character.SendPacketAsync(character.GenerateQuestPacket());
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
                });
                return false;
            }

            if (quest.LevelMax < character.Level)
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.LevelTooHigh
                });
                return false;
            }

            if (quest.IsDaily && (characterQuest.Value?.CompletedOn?.Plus(Duration.FromDays(1)) > clock.GetCurrentInstant()))
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.DailyQuestOncePerDay
                });
                return false;
            }

            character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                CharacterId = character.VisualId,
                Id = Guid.NewGuid(),
                Quest = quest,
                QuestId = quest.QuestId
            });
            await character.SendPacketAsync(character.GenerateQuestPacket(showDialog: true));
            if (quest.TargetMap != null)
            {
                await character.SendPacketAsync(quest.GenerateTargetPacket());
            }
            return true;
        }

        public Task<bool> ValidateQuestAsync(ICharacterEntity character, short questId)
        {
            var characterQuest = character.Quests.Values.FirstOrDefault(s => s.QuestId == questId);
            if (characterQuest is null)
            {
                return Task.FromResult(false);
            }

            var handler = questTypeHandlers.FirstOrDefault(h => h.QuestType == characterQuest.Quest.QuestType);
            return handler?.ValidateAsync(character, characterQuest) ?? Task.FromResult(false);
        }

        public async Task OnMonsterKilledAsync(ICharacterEntity character, NpcMonsterDto mob)
        {
            foreach (var quest in character.Quests.Values.Where(q => q.CompletedOn is null).ToList())
            {
                var handler = questTypeHandlers.FirstOrDefault(h => h.QuestType == quest.Quest.QuestType);
                if (handler == null)
                {
                    continue;
                }
                await handler.OnMonsterKilledAsync(character, mob, quest);
                if (quest.CompletedOn != null)
                {
                    await CompleteQuestAsync(character, quest);
                }
            }
        }

        // Send the UI packet trio synchronously in the fixed order the client
        // expects (QuestComplete message, final objective snapshot, quest list),
        // THEN publish the domain event for decoupled subscribers like
        // QuestChainHandler (NextQuestId) or future reward/achievement hooks.
        // Wolverine does not guarantee subscriber dispatch ordering, so the
        // packet sequence has to live here rather than in a subscriber — the
        // client crashes on out-of-order quest packets.
        public async Task CompleteQuestAsync(ICharacterEntity character, CharacterQuest quest)
        {
            quest.CompletedOn ??= clock.GetCurrentInstant();
            await character.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.QuestComplete
            });
            await character.SendPacketAsync(quest.GenerateQstiPacket(false));
            await character.SendPacketAsync(character.GenerateQuestPacket());
            await messageBus.PublishAsync(new QuestCompletedEvent(character, quest));
        }
    }
}
