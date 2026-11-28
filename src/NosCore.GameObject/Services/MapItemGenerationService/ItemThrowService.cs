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
using NosCore.Packets.ServerPackets.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class ItemThrowService(IItemGenerationService itemGenerationService,
        IMapItemGenerationService mapItemGenerationService, IRandomProvider random) : IItemThrowService
    {
        // Traced raid loot lands up to 10 cells away from its boss on either axis.
        private const int Spread = 10;
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
                    await mapInstance.SendPacketAsync(new ThrowPacket
                    {
                        VNum = pile.VNum,
                        VisualId = dropped.VisualId,
                        FromX = around.X,
                        FromY = around.Y,
                        ToX = x,
                        ToY = y,
                        Amount = dropped.Amount
                    });
                }
            }
        }

        public Task PlaceAsync(MapInstance mapInstance, Spot at, short vNum)
        {
            var placed = mapItemGenerationService.Create(mapInstance, itemGenerationService.Create(vNum, 1), at.X, at.Y);
            return mapInstance.SendPacketAsync(placed.GenerateDrop());
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
