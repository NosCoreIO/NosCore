//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class CardParser(IDao<CardDto, short> cardDao, IDao<BCardDto, short> bcardDao, ILoggerFactory loggerFactory, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        //  VNUM	CardId
        //  NAME    Name
        //
        //  GROUP	Level	0
        //  STYLE	0	0	BuffType	0	0
        //  EFFECT	0	0
        //  TIME	Duration	Delay
        //  1ST	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //  2ST	0	0	0	0	0	0	0	0	0	0	0	0
        //  LAST	0	0
        //  DESC Description
        //
        //  END
        //#========================================================
        private readonly string _fileCardDat = $"{Path.DirectorySeparatorChar}Card.dat";

        public FluentParserBuilder<CardDto> BuildParser(string folder)
        {
            return FluentParserBuilder<CardDto>.Create(folder + _fileCardDat, "END", 1)
                .Field(x => x.CardId, "VNUM", 0, 2, s => Convert.ToInt16(s), "Card vnum")
                .Field(x => x.NameI18NKey, "NAME", 0, 2, s => s, "Localization key (zts##e)")
                .Field(x => x.Level, "GROUP", 0, 3, s => Convert.ToByte(s), "Card level tier")
                .Field(x => x.EffectId, "EFFECT", 0, 2, s => Convert.ToInt32(s), "Visual effect id")
                .Field(x => x.BuffType, "STYLE", 0, 3, s => (BCardType.CardType)Convert.ToByte(s), "Buff type from STYLE column 3")
                .Field(x => x.Duration, "TIME", 0, 2, s => Convert.ToInt32(s), "Duration in deciseconds")
                .Field(x => x.Delay, "TIME", 0, 3, s => Convert.ToInt32(s), "Activation delay")
                .Field(x => x.TimeoutBuff, "LAST", 0, 2, s => Convert.ToInt16(s), "Follow-up buff id when card expires")
                .Field(x => x.TimeoutBuffChance, "LAST", 0, 3, s => Convert.ToByte(s), "% chance the follow-up buff fires")
                .Field(x => x.BCards, chunk => AddBCards(chunk),
                    source: "1ST + 2ST (5 groups of 6)", description: "Up to 5 BCards, first 3 from 1ST then 2 from 2ST");
        }

        private readonly ILogger<CardParser> _logger = loggerFactory.CreateLogger<CardParser>();

        public async Task InsertCardsAsync(string folder)
        {
            var parser = BuildParser(folder).Build(loggerFactory, logLanguage);
            var cards = (await parser.GetDtosAsync()).GroupBy(p => p.CardId).Select(g => g.First()).ToList();
            await cardDao.TryInsertOrUpdateAsync(cards);
            await bcardDao.TryInsertOrUpdateAsync(cards.Where(s => s.BCards != null).SelectMany(s => s.BCards));

            _logger.LogInformation(logLanguage[LogLanguageKey.CARDS_PARSED], cards.Count);
        }

        public List<BCardDto> AddBCards(Dictionary<string, string[][]> chunks)
        {
            var list = new List<BCardDto>();
            for (var j = 0; j < 5; j++)
            {
                var key = (j > 2) ? "2ST" : "1ST";
                var i = (j > 2) ? j - 3 : j;
                var row = chunks[key][0];
                var lastCol = i * 6 + 7;
                if (row.Length <= lastCol)
                {
                    continue;
                }

                if ((row[2 + i * 6] == "-1") || (row[2 + i * 6] == "0"))
                {
                    continue;
                }

                var first = int.Parse(row[i * 6 + 6]);
                list.Add(new BCardDto
                {
                    CardId = Convert.ToInt16(chunks["VNUM"][0][2]),
                    Type = byte.Parse(row[2 + i * 6]),
                    SubType = (byte)((Convert.ToByte(row[3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(row[7 + i * 6]) / 4,
                    ThirdData = int.Parse(row[5 + i * 6]),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = Math.Abs(first % 4) == 2
                });
            }

            return list;
        }
    }
}
