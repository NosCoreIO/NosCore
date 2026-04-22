//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.QuestService;

namespace NosCore.GameObject.Messaging.Events
{
    // Published once per quest when CompletedOn transitions from null. The quest-
    // completion packet handler sends the client-side UI packets; the quest-chain
    // handler walks NextQuestId and grants the follow-up quest. Anything else that
    // cares about a quest finishing (rewards, achievements, cutscenes) should also
    // subscribe here so the kill / move / item / dialog paths never need to know.
    public sealed record QuestCompletedEvent(ICharacterEntity Character, CharacterQuest Quest);
}
