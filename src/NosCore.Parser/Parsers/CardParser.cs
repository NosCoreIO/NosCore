//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class CardParser
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

        private readonly IDao<CardDto, short> _cardDao;
        private readonly IDao<BCardDto, short> _bcardDao;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public CardParser(IDao<CardDto, short> cardDao, IDao<BCardDto, short> bcardDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _cardDao = cardDao;
            _bcardDao = bcardDao;
            _logger = logger;
            _logLanguage = logLanguage;
        }

        public async Task InsertCardsAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(CardDto.CardId), chunk => Convert.ToInt16(chunk["VNUM"][0][2])},
                {nameof(CardDto.NameI18NKey), chunk => chunk["NAME"][0][2]},
                {nameof(CardDto.Level), chunk => Convert.ToByte(chunk["GROUP"][0][3])},
                {nameof(CardDto.EffectId), chunk => Convert.ToInt32(chunk["EFFECT"][0][2])},
                {nameof(CardDto.BuffType), chunk => (BCardType.CardType) Convert.ToByte(chunk["STYLE"][0][3])},
                {nameof(CardDto.Duration), chunk => Convert.ToInt32(chunk["TIME"][0][2])},
                {nameof(CardDto.Delay), chunk => Convert.ToInt32(chunk["TIME"][0][3])},
                {nameof(CardDto.BCards), AddBCards},
                {nameof(CardDto.TimeoutBuff), chunk => Convert.ToInt16(chunk["LAST"][0][2])},
                {nameof(CardDto.TimeoutBuffChance), chunk => Convert.ToByte(chunk["LAST"][0][3])}
            };
            var genericParser = new GenericParser<CardDto>(folder + _fileCardDat,
                "END", 1, actionList, _logger, _logLanguage);
            var cards = (await genericParser.GetDtosAsync().ConfigureAwait(false)).GroupBy(p => p.CardId).Select(g => g.First()).ToList();
            await _cardDao.TryInsertOrUpdateAsync(cards).ConfigureAwait(false);
            await _bcardDao.TryInsertOrUpdateAsync(cards.Where(s => s.BCards != null).SelectMany(s => s.BCards)).ConfigureAwait(false);

            _logger.Information(_logLanguage[LogLanguageKey.CARDS_PARSED], cards.Count);
        }

        public List<BCardDto> AddBCards(Dictionary<string, string[][]> chunks)
        {
            var list = new List<BCardDto>();
            for (var j = 0; j < 5; j++)
            {
                var key = (j > 2) ? "2ST" : "1ST";
                var i = (j > 2) ? j - 3 : j;

                if ((chunks[key][0][2 + i * 6] == "-1") || (chunks[key][0][2 + i * 6] == "0"))
                {
                    continue;
                }

                var first = int.Parse(chunks[key][0][i * 6 + 6]);
                list.Add(new BCardDto
                {
                    CardId = Convert.ToInt16(chunks["VNUM"][0][2]),
                    Type = byte.Parse(chunks[key][0][2 + i * 6]),
                    SubType = (byte)((Convert.ToByte(chunks[key][0][3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(chunks[key][0][7 + i * 6]) / 4,
                    ThirdData = int.Parse(chunks[key][0][5 + i * 6]),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = Math.Abs(first % 4) == 2
                });
            }

            return list;
        }
    }
}