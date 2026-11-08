//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService;

// Which DATA column holds what, per quest type, as the parsed quest data lays them out.
public static class QuestObjectiveLayout
{
    public static int? ItemVNum(this QuestObjectiveDto objective, QuestType type) => type switch
    {
        QuestType.Collect or QuestType.Required or QuestType.Wear => objective.SecondData,
        _ => objective.FirstData
    };

    public static int? NpcVNum(this QuestObjectiveDto objective, QuestType type) => type switch
    {
        QuestType.Brings => objective.SecondData,
        QuestType.Required => objective.FirstData,
        _ => null
    };

    public static int RequiredAmount(this QuestObjectiveDto objective, QuestType type) => (type switch
    {
        QuestType.Collect or QuestType.Brings or QuestType.Required => objective.ThirdData,
        _ => objective.SecondData
    }) ?? 0;
}
