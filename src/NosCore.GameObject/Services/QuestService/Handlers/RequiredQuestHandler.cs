//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class RequiredQuestHandler : HandInQuestHandlerBase
{
    private const byte NpcArgument = 6;

    public override QuestType QuestType => QuestType.Required;

    // Official EU trace, 2026-11-29 (quest 3250): talking without the item answers "sayi … 11 982" before the
    // NPC's dialog; with it, "sayi2 … 11 983 6 {npcVNum}" follows the item leaving the bag.
    protected override Task OnItemsMissingAsync(PlayerComponentBundle character) =>
        character.SendPacketAsync(character.GenerateSayi(SayColorType.Red, Game18NConstString.InsufficientItemsToDelivery));

    protected override Task OnHandedInAsync(PlayerComponentBundle character, short npcVNum) =>
        character.SendPacketAsync(character.GenerateSayi2(SayColorType.Red, Game18NConstString.TalkedTo, NpcArgument, npcVNum));
}
