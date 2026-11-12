//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

/// <summary>
/// Tables built once at startup, on the same model as <see cref="NpcCombatCatalog"/>:
/// NosCore.Data keeps its navigation collections internal, so the rows arrive flat from the
/// DAOs and are grouped here.
/// </summary>
public sealed class CardCatalog : ICardCatalog, ISingletonService
{
    private static readonly IReadOnlyList<BCardDto> EmptyBCards = Array.Empty<BCardDto>();

    private readonly IReadOnlyDictionary<short, CardDto> _cards;
    private readonly IReadOnlyDictionary<short, IReadOnlyList<BCardDto>> _bcardsByCard;
    private readonly IReadOnlyDictionary<short, IReadOnlyList<BCardDto>> _bcardsByItem;
    private readonly HashSet<(short CardId, ItemEffectType Effect)> _grants;

    public CardCatalog(List<CardDto> cards, List<BCardDto> bCards, List<ItemDto> items)
    {
        // Duplicate cards should not exist, but a database imported twice does happen: keep
        // the first and carry on rather than bringing the startup down.
        _cards = cards
            .GroupBy(c => c.CardId)
            .ToDictionary(g => g.Key, g => g.First());

        _bcardsByCard = bCards
            .Where(b => b.CardId.HasValue)
            .GroupBy(b => b.CardId.GetValueOrDefault())
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BCardDto>)g.ToArray());

        _bcardsByItem = bCards
            .Where(b => b.ItemVNum.HasValue)
            .GroupBy(b => b.ItemVNum.GetValueOrDefault())
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BCardDto>)g.ToArray());

        _grants = items
            .Where(i => i.Effect != ItemEffectType.NoEffect)
            .Select(i => (Card: GrantedCardId(i.VNum), i.Effect))
            .Where(g => g.Card.HasValue)
            .Select(g => (g.Card.GetValueOrDefault(), g.Effect))
            .ToHashSet();
    }

    public CardDto? GetCard(short cardId) => _cards.GetValueOrDefault(cardId);

    public IReadOnlyList<BCardDto> GetCardBCards(short cardId) =>
        _bcardsByCard.TryGetValue(cardId, out var list) ? list : EmptyBCards;

    public IReadOnlyList<BCardDto> GetItemBCards(short itemVnum) =>
        _bcardsByItem.TryGetValue(itemVnum, out var list) ? list : EmptyBCards;

    public CardDto? GetItemCard(short itemVnum) =>
        GrantedCardId(itemVnum) is { } cardId ? GetCard(cardId) : null;

    public bool IsGrantedBy(short cardId, ItemEffectType effect) => _grants.Contains((cardId, effect));

    private short? GrantedCardId(short itemVnum) =>
        (short?)GetItemBCards(itemVnum).FirstOrDefault(b => b.Type == (byte)BCardType.CardType.Buff)?.SecondData;
}
