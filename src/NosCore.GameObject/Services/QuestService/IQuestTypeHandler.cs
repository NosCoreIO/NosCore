//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService;

public interface IQuestTypeHandler
{
    QuestType QuestType { get; }

    bool CompletesWhenTaken(Quest quest) => false;

    Task<bool> ValidateAsync(PlayerComponentBundle character, CharacterQuest quest) => Task.FromResult(false);

    Task OnMonsterKilledAsync(PlayerComponentBundle character, NpcMonsterDto mob, CharacterQuest quest) => Task.CompletedTask;

    // The two NPC hooks below return true once every objective of the quest is met.
    Task<bool> OnNpcTalkedAsync(PlayerComponentBundle character, NpcMonsterDto npc, CharacterQuest quest) => Task.FromResult(false);

    Task<bool> OnQuestDialogAnsweredAsync(PlayerComponentBundle character, NpcMonsterDto npc, CharacterQuest quest) => Task.FromResult(false);

    // The capture and collect hooks return true once every objective of the quest is met.
    Task<bool> OnMonsterCapturedAsync(PlayerComponentBundle character, NpcMonsterDto mob, CharacterQuest quest) => Task.FromResult(false);

    Task<bool> OnMapEntityCollectedAsync(PlayerComponentBundle character, NpcMonsterDto mapObject, CharacterQuest quest) => Task.FromResult(false);

    Task<bool> OnEnemyPlayerDefeatedAsync(PlayerComponentBundle character, CharacterQuest quest) => Task.FromResult(false);

    // Returns true once every objective is met; instanceId is the client's time-space or raid id.
    Task<bool> OnScriptedInstanceSucceededAsync(PlayerComponentBundle character, ScriptedInstanceType type, byte instanceId,
        CharacterQuest quest) => Task.FromResult(false);
}
