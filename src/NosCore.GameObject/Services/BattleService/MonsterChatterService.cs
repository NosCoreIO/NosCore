//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

public sealed class MonsterChatterService(List<ChatterLineDto> chatterLines, IRandomProvider random,
    ISessionRegistry sessionRegistry, IGameLanguageLocalizer localizer) : IMonsterChatterService
{
    private readonly Dictionary<byte, ChatterLineDto[]> _linesByGroup = chatterLines
        .GroupBy(line => line.ChatterGroup)
        .ToDictionary(group => group.Key, group => group.OrderBy(line => line.ChatterLineId).ToArray());

    public async Task ShoutAsync(INonPlayableEntity entity)
    {
        if (entity.MapInstance == null || !_linesByGroup.TryGetValue(entity.NpcMonster.ChatterGroup, out var lines))
        {
            return;
        }

        var line = lines[random.Next(0, lines.Length)];
        if (line.Message is { } cry)
        {
            await entity.MapInstance.SendPacketAsync(entity.GenerateSayi(SayColorType.Default, (Game18NConstString)cry));
            return;
        }

        if (!Enum.TryParse<LanguageKey>(line.Text, out var text))
        {
            return;
        }

        foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(entity.MapInstance.MapInstanceId))
        {
            if (!session.TryGetCharacter(out var character))
            {
                continue;
            }

            await session.SendPacketAsync(entity.GenerateSay(localizer[text, character.AccountLanguage],
                SayColorType.Default));
        }
    }
}
