//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.GroupService;

public sealed class CoupleBuffService(IFriendHub friendHub, IBuffService buffService, ICardCatalog cardCatalog)
    : ICoupleBuffService, ISingletonService
{
    public const short CoupleCardId = 319;

    public async Task RefreshAsync(IEnumerable<ICharacterEntity> characters)
    {
        foreach (var character in characters.DistinctBy(s => s.VisualId))
        {
            var withSpouse = character.Group is { Count: > 1 } group
                && (await friendHub.GetFriendsAsync(character.VisualId))
                    .Any(s => s.RelationType == CharacterRelationType.Spouse && group.IsMember(s.CharacterId));
            var hasBuff = buffService.HasBuff(character, CoupleCardId);
            if (withSpouse && !hasBuff && cardCatalog.GetCard(CoupleCardId) is { } card)
            {
                await buffService.ApplyPermanentAsync(character, card, cardCatalog.GetCardBCards(CoupleCardId));
            }
            else if (!withSpouse && hasBuff)
            {
                await buffService.RemoveAsync(character, CoupleCardId);
            }
        }
    }
}
