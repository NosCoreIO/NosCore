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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using Serilog;

namespace NosCore.Parser.Parsers

{
    public class CardParser
    {
        private const string FileCardDat = "\\Card.dat";

        private readonly IGenericDao<CardDto> _cardDao;
        private readonly IGenericDao<BCardDto> _bcardDao;
        private readonly ILogger _logger;
        private readonly List<BCardDto> Bcards = new List<BCardDto>();
        private readonly List<CardDto> Cards = new List<CardDto>();
        private CardDto _card = new CardDto();
        private int _counter;
        private bool _itemAreaBegin;

        public CardParser(IGenericDao<CardDto> cardDao, IGenericDao<BCardDto> bcardDao, ILogger logger)
        {
            _cardDao = cardDao;
            _bcardDao = bcardDao;
            _logger = logger;
        }
        public void AddFirstData(string[] currentLine)
        {
            for (var i = 0; i < 3; i++)
            {
                if (currentLine[2 + (i * 6)] == "-1" || currentLine[2 + (i * 6)] == "0")
                {
                    continue;
                }

                var first = int.Parse(currentLine[(i * 6) + 6]);
                var bcard = new BCardDto
                {
                    CardId = _card.CardId,
                    Type = byte.Parse(currentLine[2 + (i * 6)]),
                    SubType = (byte) (((Convert.ToByte(currentLine[3 + (i * 6)]) + 1) * 10) + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(currentLine[7 + (i * 6)]) / 4,
                    ThirdData = int.Parse(currentLine[5 + (i * 6)]),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = Math.Abs(first % 4) == 2
                };
                Bcards.Add(bcard);
            }
        }

        public void AddSecondData(string[] currentLine)
        {
            for (var i = 0; i < 2; i++)
            {
                if (currentLine[2 + (i * 6)] == "-1" || currentLine[2 + (i * 6)] == "0")
                {
                    continue;
                }

                var first = int.Parse(currentLine[(i * 6) + 6]);
                var bcard = new BCardDto
                {
                    CardId = _card.CardId,
                    Type = byte.Parse(currentLine[2 + (i * 6)]),
                    SubType = (byte) (((Convert.ToByte(currentLine[3 + (i * 6)]) + 1) * 10) + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(currentLine[7 + (i * 6)]) / 4,
                    ThirdData = int.Parse(currentLine[5 + (i * 6)]),
                    IsLevelScaled = Convert.ToBoolean((uint) (first < 0 ? 0 : first) % 4),
                    IsLevelDivided = (uint) (first < 0 ? 0 : first) % 4 == 2
                };
                Bcards.Add(bcard);
            }
        }

        public void AddThirdData(string[] currentLine)
        {
            _card.TimeoutBuff = short.Parse(currentLine[2]);
            _card.TimeoutBuffChance = byte.Parse(currentLine[3]);
            // investigate
            if (_cardDao.FirstOrDefault(s => s.CardId == _card.CardId) == null)
            {
                Cards.Add(_card);
                _counter++;
            }

            _itemAreaBegin = false;
        }

        public void InsertCards(string folder)
        {
            var _line = string.Empty;
            using (var npcIdStream =
                new StreamReader(folder + FileCardDat, Encoding.Default))
            {
                while ((_line = npcIdStream.ReadLine()) != null)
                {
                    var currentLine = _line.Split('\t');

                    if (currentLine.Length > 2 && currentLine[1] == "VNUM")
                    {
                        _card = new CardDto
                        {
                            CardId = Convert.ToInt16(currentLine[2])
                        };
                        _itemAreaBegin = true;
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "GROUP")
                    {
                        if (!_itemAreaBegin)
                        {
                            continue;
                        }

                        _card.Level = Convert.ToByte(currentLine[3]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "EFFECT")
                    {
                        _card.EffectId = Convert.ToInt32(currentLine[2]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "STYLE")
                    {
                        _card.BuffType = (BCardType.CardType) Convert.ToByte(currentLine[3]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "TIME")
                    {
                        _card.Duration = Convert.ToInt32(currentLine[2]);
                        _card.Delay = Convert.ToInt32(currentLine[3]);
                    }
                    else
                    {
                        if (currentLine.Length > 3 && currentLine[1] == "1ST")
                        {
                            AddFirstData(currentLine);
                        }
                        else if (currentLine.Length > 3 && currentLine[1] == "2ST")
                        {
                            AddSecondData(currentLine);
                        }
                        else if (currentLine.Length > 3 && currentLine[1] == "LAST")
                        {
                            AddThirdData(currentLine);
                        }
                    }
                }

                _cardDao.InsertOrUpdate(Cards);
                _bcardDao.InsertOrUpdate(Bcards);

                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CARDS_PARSED),
                    _counter);
            }
        }
    }
}