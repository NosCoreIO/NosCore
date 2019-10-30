﻿//  __  _  __    __   ___ __  ___ ___
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

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
        private const string FileCardDat = "\\Card.dat";


        private readonly IGenericDao<BCardDto> _bcardDao;
        private readonly IGenericDao<CardDto> _cardDao;
        private readonly ILogger _logger;

        public CardParser(IGenericDao<CardDto> cardDao, IGenericDao<BCardDto> bcardDao, ILogger logger)
        {
            _cardDao = cardDao;
            _bcardDao = bcardDao;
            _logger = logger;
        }

        public void InsertCards(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[]>, object>>
            {
                {"CardId", chunk => Convert.ToInt16(chunk["VNUM"][2])},
                {"Level", chunk => Convert.ToByte(chunk["GROUP"][2])},
                {"EffectId", chunk => Convert.ToInt32(chunk["EFFECT"][2])},
                {"BuffType", chunk => (BCardType.CardType) Convert.ToByte(chunk["STYLE"][3])},
                {"Duration", chunk => Convert.ToInt32(chunk["TIME"][2])},
                {"Delay", chunk => Convert.ToInt32(chunk["TIME"][3])},
                //1ST
                //2ND
                {"TimeoutBuff", chunk => short.Parse(chunk["LAST"][2])},
                {"TimeoutBuffChance", chunk => short.Parse(chunk["LAST"][3])}
            };
            var genericParser = new GenericParser<CardDto>(folder + FileCardDat,
                "#========================================================", actionList);
            var cards = genericParser.GetDtos();
            _cardDao.InsertOrUpdate(cards);
            //_bcardDao.InsertOrUpdate(Bcards);

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CARDS_PARSED), cards.Count);
        }

        //public void AddFirstData(string[] currentLine)
        //{
        //    for (var i = 0; i < 3; i++)
        //    {
        //        if ((currentLine[2 + i * 6] == "-1") || (currentLine[2 + i * 6] == "0"))
        //        {
        //            continue;
        //        }

        //        var first = int.Parse(currentLine[i * 6 + 6]);
        //        var bcard = new BCardDto
        //        {
        //            CardId = _card.CardId,
        //            Type = byte.Parse(currentLine[2 + i * 6]),
        //            SubType = (byte)((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
        //            FirstData = (first > 0 ? first : -first) / 4,
        //            SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
        //            ThirdData = int.Parse(currentLine[5 + i * 6]),
        //            IsLevelScaled = Convert.ToBoolean(first % 4),
        //            IsLevelDivided = Math.Abs(first % 4) == 2
        //        };
        //        Bcards.Add(bcard);
        //    }
        //}

        //public void AddSecondData(string[] currentLine)
        //{
        //    for (var i = 0; i < 2; i++)
        //    {
        //        if ((currentLine[2 + i * 6] == "-1") || (currentLine[2 + i * 6] == "0"))
        //        {
        //            continue;
        //        }

        //        var first = int.Parse(currentLine[i * 6 + 6]);
        //        var bcard = new BCardDto
        //        {
        //            CardId = _card.CardId,
        //            Type = byte.Parse(currentLine[2 + i * 6]),
        //            SubType = (byte)((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
        //            FirstData = (first > 0 ? first : -first) / 4,
        //            SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
        //            ThirdData = int.Parse(currentLine[5 + i * 6]),
        //            IsLevelScaled = Convert.ToBoolean((uint)(first < 0 ? 0 : first) % 4),
        //            IsLevelDivided = (uint)(first < 0 ? 0 : first) % 4 == 2
        //        };
        //        Bcards.Add(bcard);
        //    }
        //}
    }
}