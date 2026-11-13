//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // Family missions and achievements are quest.dat rows 9000-9199 and 9400-9499, laid out as
    // VNUM {id} {name item} {kind} {category} {sub-type} {?}. Their rewards are items named after
    // what they give ("1000 FXP"), so the experience is read from the English item names.
    public partial class FamilyQuestParser(IDao<FamilyQuestDto, short> familyQuestDao, ILoggerFactory loggerFactory,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly ILogger<FamilyQuestParser> _logger = loggerFactory.CreateLogger<FamilyQuestParser>();
        private readonly string _fileQuestDat = $"{Path.DirectorySeparatorChar}quest.dat";
        private Dictionary<short, string> _itemNames = new();

        public static bool IsFamilyQuest(short questId) => questId is >= 9000 and <= 9199 or >= 9400 and <= 9499;

        public FluentParserBuilder<FamilyQuestDto> BuildParser(string folder)
        {
            return FluentParserBuilder<FamilyQuestDto>.Create(folder + _fileQuestDat, "END", 0)
                .Field(x => x.QuestId, chunk => Convert.ToInt16(chunk["VNUM"][0][1]), [("VNUM", 0, 1)], description: "Quest id")
                .Field(x => x.NameItemVNum, chunk => Number(chunk["VNUM"][0][2]) is { } item ? (short)item : (short)0,
                    [("VNUM", 0, 2)], description: "Item whose name the family window shows")
                .Field(x => x.Kind, chunk => (FamilyQuestKind)(Number(chunk["VNUM"][0][3]) ?? 0),
                    [("VNUM", 0, 3)], description: "0 mission, 1 achievement")
                .Field(x => x.Objective, chunk => Objective(chunk),
                    [("VNUM", 0, 3), ("VNUM", 0, 4), ("VNUM", 0, 5)], description: "Kind, category and sub-type pick what the quest counts")
                .Field(x => x.FamilyLevelMinimum, chunk => (byte)Math.Max(0, Number(chunk["LEVEL"][0][1]) ?? 0),
                    [("LEVEL", 0, 1)], description: "Family level needed")
                .Field(x => x.Target, chunk => Number(chunk["DATA"][0][1]) ?? 0, [("DATA", 0, 1)], description: "Count to reach")
                .Field(x => x.Parameter, chunk => Number(chunk["DATA"][0][2]), [("DATA", 0, 2)],
                    description: "What is counted, such as the family level of a level achievement")
                .Field(x => x.PreviousQuestId, chunk => Number(chunk["LINK"][0][1]) is { } previous ? (short)previous : null,
                    [("LINK", 0, 1)], description: "Achievement to unlock first")
                .Field(x => x.Experience, chunk => Enumerable.Range(1, 4)
                        .Select(i => Number(chunk["PRIZE"][0][i]))
                        .Sum(prize => prize is { } vnum ? Experience((short)vnum) : 0),
                    [("PRIZE", 0, 1), ("PRIZE", 0, 2), ("PRIZE", 0, 3), ("PRIZE", 0, 4)],
                    description: "Family experience from the reward items named \"<n> FXP\"");
        }

        public async Task InsertFamilyQuestsAsync(string folder)
        {
            var text = ItemDescriptions.ReadText(folder);
            _itemNames = ItemDescriptions.ReadNameAndDescriptionKeys(folder)
                .Where(s => text.ContainsKey(s.Value.Name))
                .ToDictionary(s => s.Key, s => text[s.Value.Name]);
            var parser = BuildParser(folder).Build(loggerFactory, logLanguage);
            var quests = (await parser.GetDtosAsync()).Where(s => IsFamilyQuest(s.QuestId)).ToList();
            await familyQuestDao.TryInsertOrUpdateAsync(quests);
            _logger.LogInformation(logLanguage[LogLanguageKey.FAMILY_QUESTS_PARSED], quests.Count);
        }

        private static FamilyQuestObjective? Objective(Dictionary<string, string[][]> chunk) =>
            (Number(chunk["VNUM"][0][3]), Number(chunk["VNUM"][0][4]), Number(chunk["VNUM"][0][5])) switch
            {
                ((int)FamilyQuestKind.Achievement, 0, 0) => FamilyQuestObjective.FamilyLevel,
                ((int)FamilyQuestKind.Achievement, 0, 1) => FamilyQuestObjective.DailyMessage,
                _ => null
            };

        private int Experience(short prize) =>
            _itemNames.TryGetValue(prize, out var name) && ExperienceName().Match(name) is { Success: true } match
                ? int.Parse(match.Groups[1].Value)
                : 0;

        private static int? Number(string value) => int.TryParse(value, out var number) && number != -1 ? number : null;

        [GeneratedRegex(@"^(\d+) FXP$")]
        private static partial Regex ExperienceName();
    }
}
