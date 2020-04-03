using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.Enumerations.Quest;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Providers.QuestProvider
{
    public class QuestData
    {
        public QuestType QuestType { get; set; }

        public int FirstData { get; set; }

        public int? SecondData { get; set; }

        public int? ThirdData { get; set; }

        public int? FourthData { get; set; }
    }
}