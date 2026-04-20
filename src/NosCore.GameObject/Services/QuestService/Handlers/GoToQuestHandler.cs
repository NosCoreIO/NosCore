//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class GoToQuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.GoTo;

    public async Task<bool> ValidateAsync(ICharacterEntity character, CharacterQuest quest)
    {
        var targetX = quest.Quest.TargetX ?? 0;
        var targetY = quest.Quest.TargetY ?? 0;
        var targetMap = quest.Quest.TargetMap ?? 0;

        var inRange = character.MapX <= targetX + 5 && character.MapX >= targetX - 5
            && character.MapY <= targetY + 5 && character.MapY >= targetY - 5
            && character.MapId == targetMap;
        if (!inRange)
        {
            return false;
        }

        await character.SendPacketAsync(quest.Quest.GenerateTargetOffPacket());
        return true;
    }
}
