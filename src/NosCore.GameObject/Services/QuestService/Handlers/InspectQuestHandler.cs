//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// FirstData: the map object to inspect, SecondData: the item it yields, ThirdData: how
// many times. The item itself comes from the object's own drop, not from the quest.
[UsedImplicitly]
public sealed class InspectQuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.Inspect;

    public Task<bool> OnNpcTalkedAsync(PlayerComponentBundle character, NpcMonsterDto npc, CharacterQuest quest) =>
        NpcObjectiveProgress.RecordAsync(character, quest, npc.NpcMonsterVNum, NpcObjectiveProgress.Times);
}
