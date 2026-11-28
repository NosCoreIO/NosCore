//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class RequiredQuestHandler : HandInQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Required;

    public override async Task<ItemQuestProgress> OnNpcDialogRequestedAsync(PlayerComponentBundle character, CharacterQuest quest, short npcVNum)
    {
        var progress = await base.OnNpcDialogRequestedAsync(character, quest, npcVNum);
        if (progress != ItemQuestProgress.None)
        {
            await character.SendPacketAsync(character.TalkedToLine(npcVNum));
        }

        return progress;
    }
}
