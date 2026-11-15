// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.Networking;

namespace NosCore.GameObject.Services.BattleService;

public interface ISummonService
{
    // Spawns, within a few cells of the source, every monster its summon cards call for.
    Task SummonAsync(IAliveEntity source, IEnumerable<BCardDto> cards);
}

public sealed class SummonService(List<NpcMonsterDto> npcMonsters, IRandomProvider random) : ISummonService, ISingletonService
{
    public Task SummonAsync(IAliveEntity source, IEnumerable<BCardDto> cards)
    {
        var summons = new List<NpcMonsterDto>();
        foreach (var card in cards)
        {
            var count = card.Effect() switch
            {
                BCardEffect.SummonsSummonUponDeath or BCardEffect.SummonsSummons => card.FirstData,
                BCardEffect.SummonsSummonUponDeathChance or BCardEffect.SummonsSummonningChance
                    => random.Next(0, 100) < card.FirstData ? 1 : 0,
                _ => 0,
            };
            if (count <= 0 || npcMonsters.Find(m => m.NpcMonsterVNum == card.SecondData) is not { } summoned)
            {
                continue;
            }

            summons.AddRange(Enumerable.Repeat(summoned, count));
        }

        if (summons.Count == 0 || source.MapInstance == null)
        {
            return Task.CompletedTask;
        }

        return source.OnMapAsync(async () =>
        {
            var map = source.MapInstance;
            foreach (var npcMonster in summons)
            {
                var (x, y) = CellNear(map.Map, source.PositionX, source.PositionY);
                var monster = map.SpawnMonster(npcMonster, x, y, true);
                await map.SendPacketAsync(monster.GenerateIn());
            }
        });
    }

    private const int Spread = 3;

    private (short X, short Y) CellNear(Map.Map map, short x, short y)
    {
        var cell = ((short)(x + random.Next(-Spread, Spread + 1)), (short)(y + random.Next(-Spread, Spread + 1)));
        return map.IsWalkable(cell.Item1, cell.Item2) ? cell : (x, y);
    }
}
