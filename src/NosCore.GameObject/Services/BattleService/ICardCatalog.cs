//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService;

/// <summary>
/// <b>Cards</b> are the game's buffs: poisons, stuns, heals over time, defence boosts.
/// Each has an id, a duration, a nature (good, bad, neutral) and a handful of BCards that
/// describe the effect.
///
/// A catalogue is needed because skills do not carry the effect with them: they carry a BCard of type
/// <c>Buff</c> saying "apply Card number N". Without a way back from N to the Card, that
/// reference is a dead letter - and it is the most common case of all, a third of the effects of
/// every skill in the game.
/// </summary>
public interface ICardCatalog
{
    /// <summary>The Card with that id, or null if it does not exist.</summary>
    CardDto? GetCard(short cardId);

    /// <summary>That Card's effects. An empty list if the Card does not exist or has none.</summary>
    IReadOnlyList<BCardDto> GetCardBCards(short cardId);

    /// <summary>
    /// The effects an item declares.
    ///
    /// A catalogue is needed because the item does not carry them: NosCore.Data keeps the
    /// navigation collections internal, so the rows arrive flat from the DAOs and must be grouped
    /// once at startup - the same reason NpcCombatCatalog and the lookup by
    /// Card above exist.
    /// </summary>
    IReadOnlyList<BCardDto> GetItemBCards(short itemVnum);
}
