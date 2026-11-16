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
    // monster.dat ETC c2 groups monsters that shout alike, but no client file holds what they
    // say, so each group takes the client's own cries that fit it.
    public class ChatterLineParser(IDao<ChatterLineDto, short> chatterLineDao, ILogger<ChatterLineParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public static readonly IReadOnlyDictionary<byte, Game18NConstString[]> Lines = new Dictionary<byte, Game18NConstString[]>
        {
            [1] = [Game18NConstString.Grrrrrrrrrrr, Game18NConstString.KeeeKeeee, Game18NConstString.Ggggggggggrrrrrrrrrrrrrrrr],
            [2] = [Game18NConstString.Huh, Game18NConstString.Uurk],
            [3] = [Game18NConstString.BeginAttack, Game18NConstString.WoahhWoahhh],
            [4] = [Game18NConstString.GoingToWipeYouAll, Game18NConstString.Urrgh, Game18NConstString.Grrrrrrrrrrr],
            [5] = [Game18NConstString.GoingToWipeYouAll, Game18NConstString.Urrgh, Game18NConstString.Grrrrrrrrrrr],
            [8] = [Game18NConstString.Hehe, Game18NConstString.BoooBooo],
            [9] = [Game18NConstString.BeginAttack, Game18NConstString.DontMove],
            [11] = [Game18NConstString.BeginAttack, Game18NConstString.DontMove],
            [94] = [Game18NConstString.DontMove, Game18NConstString.GetOverHere],
            [95] = [Game18NConstString.DontMove, Game18NConstString.GetOverHere],
            [96] = [Game18NConstString.BoooBooo],
            [97] = [Game18NConstString.BoooBooo],
            [99] = [Game18NConstString.BoooBooo],
        };

        public async Task InsertChatterLinesAsync()
        {
            short id = 0;
            var lines = Lines
                .SelectMany(group => group.Value.Select(message => new ChatterLineDto
                {
                    ChatterLineId = ++id,
                    ChatterGroup = group.Key,
                    Message = (short)message,
                }))
                .ToList();
            await chatterLineDao.TryInsertOrUpdateAsync(lines).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.CHATTER_LINES_PARSED], lines.Count);
        }
    }
}
