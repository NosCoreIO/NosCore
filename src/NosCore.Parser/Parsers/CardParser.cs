using NosCore.Core.Logger;
using NosCore.Data;
using NosCore.Domain.Buff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NosCore.Parser

{
    public class CardParser
    {
        private static string _line;
        private static int _counter = 0;
        private static CardDTO _card = new CardDTO();
        private static bool _itemAreaBegin = false;
        private static List<CardDTO> _cards = new List<CardDTO>();
        private static readonly string FileCardDat = $"\\Card.dat";
        private static readonly List<BCardDTO> Bcards = new List<BCardDTO>();

        public void AddFirstData(string[] currentLine)
        {
            for (int i = 0; i < 3; i++)
            {
                if (currentLine[2 + i * 6] == "-1" || currentLine[2 + i * 6] == "0")
                {
                    continue;
                }
                int first = int.Parse(currentLine[i * 6 + 6]);
                BCardDTO bcard = new BCardDTO
                {
                    CardId = _card.CardId,
                    Type = byte.Parse(currentLine[2 + i * 6]),
                    SubType = (byte)((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
                    ThirdData = int.Parse(currentLine[5 + i * 6]),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = Math.Abs(first % 4) == 2,
                };
                Bcards.Add(bcard);
            }
        }

        public void AddSecondData(string[] currentLine)
        {
            for (int i = 0; i < 2; i++)
            {
                if (currentLine[2 + i * 6] == "-1" || currentLine[2 + i * 6] == "0")
                {
                    continue;
                }
                int first = int.Parse(currentLine[i * 6 + 6]);
                BCardDTO bcard = new BCardDTO
                {
                    CardId = _card.CardId,
                    Type = byte.Parse(currentLine[2 + i * 6]),
                    SubType = (byte)((Convert.ToByte(currentLine[3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(currentLine[7 + i * 6]) / 4,
                    ThirdData = int.Parse(currentLine[5 + i * 6]),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = (first % 4) == 2,
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
                _cards.Add(_card);
                _counter++;
            }
            _itemAreaBegin = false;
            
        }

        public void InsertCards()
        {
            using (StreamReader npcIdStream = new StreamReader(FileCardDat, CodePagesEncodingProvider.Instance.GetEncoding(1252)))
            {
                while ((_line = npcIdStream.ReadLine()) != null)
                {
                    string[] currentLine = _line.Split('\t');

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
                        _card.BuffType = (BCardType.CardType)Convert.ToByte(currentLine[3]);
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
                DAOFactory.CardDAO.InsertOrUpdate(_cards);
                DAOFactory.BcardDAO.InsertOrUpdate(Bcards);

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey("CARDS_PARSED"), _counter));
                npcIdStream.Close();
            }
        }

    }
}
