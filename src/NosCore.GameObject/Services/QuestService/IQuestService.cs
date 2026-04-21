//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.QuestService
{
    public interface IQuestService
    {
        Task RunScriptAsync(ICharacterEntity character);
        Task RunScriptAsync(ICharacterEntity character, ScriptClientPacket? packet);
        Task OnMonsterKilledAsync(ICharacterEntity character, NpcMonsterDto mob);
        Task<bool> AddQuestAsync(ICharacterEntity character, QuestActionType type, short questId);

        // Flips CompletedOn (if not already), sends the client-side UI packet
        // trio in the exact order the client expects, and publishes
        // QuestCompletedEvent for decoupled subscribers (chain, rewards...).
        // Subscribers to other domain events (move, kill, item-used) should
        // call this once they have decided a quest is done.
        Task CompleteQuestAsync(ICharacterEntity character, CharacterQuest quest);
    }
}
