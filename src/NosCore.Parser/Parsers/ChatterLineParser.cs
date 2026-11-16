//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // monster.dat ETC c2 groups monsters that shout alike. No client file holds what they say:
    // groups heard in packet traces shout the traced lines, the others the client's own cries.
    public class ChatterLineParser(IDao<ChatterLineDto, short> chatterLineDao, ILogger<ChatterLineParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public static readonly IReadOnlyDictionary<byte, ChatterText[]> Lines = new Dictionary<byte, ChatterText[]>
        {
            [1] = [Game18NConstString.Grrrrrrrrrrr, Game18NConstString.KeeeKeeee, Game18NConstString.Ggggggggggrrrrrrrrrrrrrrrr],
            [2] =
            [
                LanguageKey.CHATTER_YOU_DARE_MESS_WITH_US,
                LanguageKey.CHATTER_WHY_TORTURE_US,
                LanguageKey.CHATTER_GRR,
                LanguageKey.CHATTER_NOBODY_CONTROLS_US,
                LanguageKey.CHATTER_WONT_BE_PUSHED_AROUND,
                LanguageKey.CHATTER_IMPROVE_WORKING_CONDITIONS,
                LanguageKey.CHATTER_NOT_YOUR_SLAVES,
                LanguageKey.CHATTER_HUMANS_KILL_US,
                LanguageKey.CHATTER_HUMANS_BEG_FOR_MERCY,
            ],
            [3] =
            [
                LanguageKey.CHATTER_VICTORY,
                LanguageKey.CHATTER_FOR_THE_KOARENS,
                LanguageKey.CHATTER_KOARENS_WONT_FORGIVE,
                LanguageKey.CHATTER_KOVOLTS_ARE_FREE,
                LanguageKey.CHATTER_SHOW_US,
            ],
            [4] =
            [
                LanguageKey.CHATTER_GROWL,
                LanguageKey.CHATTER_LONG_GROWL,
            ],
            [5] =
            [
                LanguageKey.CHATTER_GROWL_WOOF,
                LanguageKey.CHATTER_SNIFF_HEHE,
            ],
            [8] =
            [
                LanguageKey.CHATTER_LOOK_AT_THE_DIRT,
                LanguageKey.CHATTER_NEW_DRESS,
            ],
            [9] =
            [
                LanguageKey.CHATTER_CATCH_IT_FOR_A_BONUS,
                LanguageKey.CHATTER_SHOW_WHAT_YOU_CAN_DO,
                LanguageKey.CHATTER_KILL_THEM_ALL,
            ],
            [11] = [Game18NConstString.BeginAttack, Game18NConstString.DontMove],
            [92] =
            [
                LanguageKey.CHATTER_ATTACK,
                LanguageKey.CHATTER_YOU_GOT_LUCKY,
            ],
            [94] =
            [
                LanguageKey.CHATTER_ATTACK,
                LanguageKey.CHATTER_HERE_WE_GO,
                LanguageKey.CHATTER_AAAAH,
                LanguageKey.CHATTER_FOR_NOSVILLE,
                LanguageKey.CHATTER_FOR_THE_VILLAGE,
            ],
            [95] =
            [
                LanguageKey.CHATTER_HURRY_UP,
                LanguageKey.CHATTER_WRETCH,
                LanguageKey.CHATTER_WHEN_WILL_IT_END,
                LanguageKey.CHATTER_CROAK_SIP,
                LanguageKey.CHATTER_DONT_ANNOY_ME,
                LanguageKey.CHATTER_CAN_WE_GET_A_BONUS,
                LanguageKey.CHATTER_GET_AWAY_FROM_ME,
            ],
            [96] =
            [
                LanguageKey.CHATTER_FIGHT_TOGETHER,
                LanguageKey.CHATTER_TOO_LATE_TO_LEAVE,
            ],
            [97] =
            [
                LanguageKey.CHATTER_CRUSH_GLACERUS,
                LanguageKey.CHATTER_REST_AFTER_GLACERUS,
            ],
            [99] =
            [
                LanguageKey.CHATTER_FIGHT_SIDE_BY_SIDE,
                LanguageKey.CHATTER_GLACERUS_MUST_FALL,
            ],
        };

        public async Task InsertChatterLinesAsync()
        {
            short id = 0;
            var lines = Lines
                .SelectMany(group => group.Value.Select(message => new ChatterLineDto
                {
                    ChatterLineId = ++id,
                    ChatterGroup = group.Key,
                    Message = message.Cry is { } cry ? (short)cry : null,
                    Text = message.Text?.ToString(),
                }))
                .ToList();
            await chatterLineDao.TryInsertOrUpdateAsync(lines).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.CHATTER_LINES_PARSED], lines.Count);
        }
    }

    public readonly record struct ChatterText(Game18NConstString? Cry, LanguageKey? Text)
    {
        public static implicit operator ChatterText(Game18NConstString cry) => new(cry, null);

        public static implicit operator ChatterText(LanguageKey text) => new(null, text);
    }
}
