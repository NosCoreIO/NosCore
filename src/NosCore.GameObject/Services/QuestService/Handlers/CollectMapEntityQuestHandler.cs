//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// FirstData: the map object to collect, ThirdData: how many times.
[UsedImplicitly]
public sealed class CollectMapEntityQuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.CollectMapEntity;

    public Task<bool> OnMapEntityCollectedAsync(ICharacterEntity character, NpcMonsterDto mapObject, CharacterQuest quest) =>
        NpcObjectiveProgress.RecordAsync(character, quest, mapObject.NpcMonsterVNum, NpcObjectiveProgress.Times);
}
