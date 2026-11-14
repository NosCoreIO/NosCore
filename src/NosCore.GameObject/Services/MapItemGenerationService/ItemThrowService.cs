//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class ItemThrowService(IItemGenerationService itemGenerationService,
        IMapItemGenerationService mapItemGenerationService, IRandomProvider random) : IItemThrowService
    {
        private const int Spread = 4;
        private const int Attempts = 10;

        public async Task ThrowAsync(MapInstance mapInstance, Spot around, IEnumerable<ItemPile> piles)
        {
            foreach (var pile in piles)
            {
                for (var i = 0; i < pile.Piles; i++)
                {
                    var (x, y) = Landing(mapInstance, around);
                    var item = itemGenerationService.Create(pile.VNum, (short)Math.Min(pile.Amount, short.MaxValue));
                    var dropped = mapItemGenerationService.Create(mapInstance, item, x, y);
                    await mapInstance.SendPacketAsync(dropped.GenerateDrop());
                }
            }
        }

        private (short X, short Y) Landing(MapInstance mapInstance, Spot around)
        {
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                var x = (short)(around.X + random.Next(-Spread, Spread + 1));
                var y = (short)(around.Y + random.Next(-Spread, Spread + 1));
                if (mapInstance.Map.IsWalkable(x, y))
                {
                    return (x, y);
                }
            }

            return (around.X, around.Y);
        }
    }
}
