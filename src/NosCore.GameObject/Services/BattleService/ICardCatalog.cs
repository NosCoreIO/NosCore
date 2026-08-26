//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService;

// Skills reference an effect by id rather than carrying it, so the lookup back from id to Card
// has to live somewhere. The DAOs hand the rows over flat, so they are grouped once at startup.
public interface ICardCatalog
{
    /// <summary>The Card with that id, or null if it does not exist.</summary>
    CardDto? GetCard(short cardId);

    /// <summary>That Card's effects. An empty list if the Card does not exist or has none.</summary>
    IReadOnlyList<BCardDto> GetCardBCards(short cardId);

    /// <summary>The effects an item declares.</summary>
    IReadOnlyList<BCardDto> GetItemBCards(short itemVnum);
}
