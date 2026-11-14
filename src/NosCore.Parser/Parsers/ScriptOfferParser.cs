//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // qstnpc.dat rows are {npcVNum} {kind} {id} {requiredId} {levelMin} [{?}]; kind 0 offers a story
    // chapter of tutorial.dat. The NPC's chapter list is the npctalk.dat dialog holding its
    // "n_run 3001 {scriptId}" buttons.
    public class ScriptOfferParser(IDao<ScriptOfferDto, byte> scriptOfferDao, ILogger<ScriptOfferParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const string StoryChapter = "0";
        private readonly string _fileQstNpcDat = $"{Path.DirectorySeparatorChar}qstnpc.dat";
        private readonly string _fileNpcTalkDat = $"{Path.DirectorySeparatorChar}npctalk.dat";

        public async Task InsertScriptOffersAsync(string folder)
        {
            var listDialogs = await ReadListDialogsAsync(folder + _fileNpcTalkDat);
            var offers = new List<ScriptOfferDto>();
            foreach (var split in await ReadRowsAsync(folder + _fileQstNpcDat))
            {
                if (split.Length < 5 || split[1] != StoryChapter
                    || !byte.TryParse(split[2], out var scriptId)
                    || !listDialogs.TryGetValue(scriptId, out var dialogId))
                {
                    continue;
                }

                offers.Add(new ScriptOfferDto
                {
                    ScriptId = scriptId,
                    NpcVNum = Convert.ToInt16(split[0]),
                    RequiredScriptId = Convert.ToByte(split[3]),
                    LevelMinimum = Convert.ToByte(split[4]),
                    DialogId = dialogId,
                });
            }

            await scriptOfferDao.TryInsertOrUpdateAsync(offers);
            logger.LogInformation(logLanguage[LogLanguageKey.SCRIPT_OFFERS_PARSED], offers.Count);
        }

        private static async Task<List<string[]>> ReadRowsAsync(string path)
        {
            var rows = new List<string[]>();
            using var stream = new StreamReader(path, Encoding.Default);
            while (await stream.ReadLineAsync() is { } line)
            {
                if (!line.StartsWith('#'))
                {
                    rows.Add(line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
                }
            }

            return rows;
        }

        private static async Task<Dictionary<byte, short>> ReadListDialogsAsync(string path)
        {
            var dialogs = new Dictionary<byte, short>();
            short dialogId = 0;
            foreach (var split in await ReadRowsAsync(path))
            {
                if (split is ["%", var id, ..] && short.TryParse(id, out var header))
                {
                    dialogId = header;
                }
                else if (split is ["b", .., "n_run", "3001", var script] && byte.TryParse(script, out var scriptId))
                {
                    dialogs.TryAdd(scriptId, dialogId);
                }
            }

            return dialogs;
        }
    }
}
