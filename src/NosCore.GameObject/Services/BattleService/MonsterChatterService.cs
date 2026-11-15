//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

public sealed class MonsterChatterService(List<ChatterLineDto> chatterLines, IRandomProvider random) : IMonsterChatterService
{
    private readonly Dictionary<byte, Game18NConstString[]> _linesByGroup = chatterLines
        .GroupBy(line => line.ChatterGroup)
        .ToDictionary(group => group.Key, group => group.OrderBy(line => line.ChatterLineId).Select(line => (Game18NConstString)line.Message).ToArray());

    public Task ShoutAsync(INonPlayableEntity entity)
    {
        if (entity.MapInstance == null || !_linesByGroup.TryGetValue(entity.NpcMonster.ChatterGroup, out var lines))
        {
            return Task.CompletedTask;
        }

        var line = lines[random.Next(0, lines.Length)];
        return entity.MapInstance.SendPacketAsync(entity.GenerateSayi(SayColorType.Default, line));
    }
}
