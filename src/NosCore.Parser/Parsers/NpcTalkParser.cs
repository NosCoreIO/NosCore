//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;

namespace NosCore.Parser.Parsers
{
    //% {DialogId}
    //t {Title}
    //s {ShopId}
    //c<list>
    //c<end>
    //b zts17695e  . n_talk 11 0&@
    //b zts3677e  . shopping 3 0
    //b zts7087e  . n_run 14 0
    //b zts3678e  . n_talk 1 0
    //b zts16037e  . n_talk 8 0&@
    //b zts16038e  . n_talk 7 0&@
    //b zts4759e  . n_run 3000 0
    //b zts4760e  . n_talk 100 0
    public class NpcTalkParser(IDao<NpcTalkDto, short> npcTalkDao, IDao<NpcTalkButtonDto, Guid> npcTalkButtonDao,
        ILoggerFactory loggerFactory, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly ILogger<NpcTalkParser> _logger = loggerFactory.CreateLogger<NpcTalkParser>();
        private readonly string _fileNpcTalkDat = $"{Path.DirectorySeparatorChar}npctalk.dat";


        public async Task ParseAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(NpcTalkDto.DialogId), chunk => Convert.ToInt16(chunk["%"][0][1])},
                {nameof(NpcTalkDto.NameI18NKey), chunk => chunk["t"][0][1]},
            };

            var genericParser = new GenericParser<NpcTalkDto>(folder + _fileNpcTalkDat,
                "%", 0, actionList, loggerFactory.CreateLogger<GenericParser<NpcTalkDto>>(), logLanguage);
            var npcTalks = (await genericParser.GetDtosAsync(" ")).ToList();
            npcTalks.Add(new NpcTalkDto { DialogId = 99, NameI18NKey = "" });
            await npcTalkDao.TryInsertOrUpdateAsync(npcTalks).OrThrowAsync();

            _logger.LogInformation(logLanguage[LogLanguageKey.NPCTALKS_PARSED], npcTalks.Count);

            await InsertButtonsAsync(folder + _fileNpcTalkDat, npcTalks.Select(t => t.DialogId).ToHashSet());
        }

        // Only the buttons that run an n_run or open another dialog are kept. A stored button is
        // matched by what it does, so parsing again keeps its id, and one no longer in the file goes.
        private async Task InsertButtonsAsync(string path, IReadOnlySet<short> dialogs)
        {
            var parsed = (await ReadButtonsAsync(path)).Where(b => dialogs.Contains(b.DialogId)).Distinct().ToList();
            var stored = npcTalkButtonDao.LoadAll()
                .GroupBy(Key)
                .ToDictionary(g => g.Key, g => g.First().NpcTalkButtonId);
            var buttons = parsed.Select(b => new NpcTalkButtonDto
            {
                NpcTalkButtonId = stored.TryGetValue(b, out var id) ? id : Guid.NewGuid(),
                DialogId = b.DialogId,
                Runner = b.Runner,
                Argument = b.Argument,
                NextDialogId = b.NextDialogId,
            }).ToList();
            var kept = buttons.Select(b => b.NpcTalkButtonId).ToHashSet();
            await npcTalkButtonDao.TryDeleteAsync(npcTalkButtonDao.LoadAll()
                .Where(b => !kept.Contains(b.NpcTalkButtonId)).Select(b => b.NpcTalkButtonId).ToList()).OrThrowAsync();
            await npcTalkButtonDao.TryInsertOrUpdateAsync(buttons).OrThrowAsync();

            _logger.LogInformation(logLanguage[LogLanguageKey.NPC_TALK_BUTTONS_PARSED], buttons.Count);
        }

        private static (short DialogId, NrunRunnerType? Runner, short Argument, short? NextDialogId) Key(NpcTalkButtonDto button) =>
            (button.DialogId, button.Runner, button.Argument, button.NextDialogId);

        private static async Task<List<(short DialogId, NrunRunnerType? Runner, short Argument, short? NextDialogId)>> ReadButtonsAsync(string path)
        {
            var buttons = new List<(short, NrunRunnerType?, short, short?)>();
            short? dialog = null;
            using var stream = new StreamReader(path, Encoding.Default);
            while (await stream.ReadLineAsync() is { } line)
            {
                var split = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (split is ["%", var id, ..])
                {
                    dialog = short.TryParse(id, out var header) ? header : null;
                    continue;
                }

                if (dialog is not { } dialogId || split is not ["b", ..])
                {
                    continue;
                }

                var command = Array.FindIndex(split, t => t is "n_run" or "n_talk");
                if (command < 0 || command + 2 >= split.Length
                    || LeadingNumber(split[command + 1]) is not { } value || LeadingNumber(split[command + 2]) is not { } argument)
                {
                    continue;
                }

                if (split[command] == "n_talk")
                {
                    buttons.Add((dialogId, null, 0, value));
                }
                else if (Enum.IsDefined((NrunRunnerType)value))
                {
                    buttons.Add((dialogId, (NrunRunnerType)value, argument, null));
                }
            }

            return buttons;
        }

        // A button's last field can carry a region marker after the number, as in "0&@KR".
        private static short? LeadingNumber(string token)
        {
            var length = token.StartsWith('-') ? 1 : 0;
            while (length < token.Length && char.IsAsciiDigit(token[length]))
            {
                length++;
            }

            return short.TryParse(token.AsSpan(0, length), out var number) ? number : null;
        }

    }
}
