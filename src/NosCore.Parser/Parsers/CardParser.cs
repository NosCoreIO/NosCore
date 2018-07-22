using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Buff;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers

{
    public class CardParser
    {
        private const string FileCardDat = "\\Card.dat";
        private static string _line;
        private static int _counter;
        private static CardDTO _card = new CardDTO();
        private static bool _itemAreaBegin;
        private static readonly List<CardDTO> Cards = new List<CardDTO>();
        private static readonly List<BCardDTO> Bcards = new List<BCardDTO>();
        private string _folder;

        public void AddFirstData(string[] currentLine)
        {
            for (var i = 0; i < 3; i++)
            {
                if (currentLine[2 + i * 6] == "-1" || currentLine[2 + i * 6] == "0")
                {
                    continue;
                }

                var first = int.Parse(currentLine[i * 6 + 6]);
                var bcard = new BCardDTO
                {
                    CardId = _card.CardId,
                    Type = byte.Parse(currentLine[2 + i * 6]),
                    SubType = (byte) ((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
                    ThirdData = int.Parse(currentLine[5 + i * 6]),
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
                if (currentLine[2 + i * 6] == "-1" || currentLine[2 + i * 6] == "0")
                {
                    continue;
                }

                var first = int.Parse(currentLine[i * 6 + 6]);
                var bcard = new BCardDTO
                {
                    CardId = _card.CardId,
                    Type = byte.Parse(currentLine[2 + i * 6]),
                    SubType = (byte) ((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
                    ThirdData = int.Parse(currentLine[5 + i * 6]),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = first % 4 == 2
                };
                Bcards.Add(bcard);
            }
        }

        public void AddThirdData(string[] currentLine)
        {
            _card.TimeoutBuff = short.Parse(currentLine[2]);
            _card.TimeoutBuffChance = byte.Parse(currentLine[3]);
            // investigate
            if (DAOFactory.CardDAO.FirstOrDefault(s => s.CardId == _card.CardId) == null)
            {
                Cards.Add(_card);
                _counter++;
            }

            _itemAreaBegin = false;
        }

        public void InsertCards(string folder)
        {
            _folder = folder;

            using (var npcIdStream =
                new StreamReader(_folder + FileCardDat, Encoding.Default))
            {
                while ((_line = npcIdStream.ReadLine()) != null)
                {
                    var currentLine = _line.Split('\t');

                    if (currentLine.Length > 2 && currentLine[1] == "VNUM")
                    {
                        _card = new CardDTO
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

                DAOFactory.CardDAO.InsertOrUpdate(Cards);
                DAOFactory.BcardDAO.InsertOrUpdate(Bcards);

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CARDS_PARSED),
                    _counter));
            }
        }
    }
}