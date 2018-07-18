using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Parser.Parsers
{
    public class DropParser
    {
        public void InsertDrop()
        {
            var drops = new List<DropDTO>();
            // Act 1
            drops.Add(new DropDTO
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 12000,
                MapTypeId = (short) MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                VNum = 2015,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                VNum = 2016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                VNum = 2023,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                VNum = 2024,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                VNum = 2028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act1
            });

            // Act2
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 80,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 80,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 80,
                MapTypeId = (short) MapTypeEnum.Oasis
            });

            // Act3
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 4000,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 350,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act3
            });

            // Act3.2 (Midgard)
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 20,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 20,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 60,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 40,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 60,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 40,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2600,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 2605,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 5857,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act32
            });


            // Act 3.4 Oasis 
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                VNum = 5999,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Oasis
            });

            // Act4
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1010,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 2,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1241,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1246,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1247,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1248,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 1429,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 2307,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                VNum = 2308,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act4
            });

            //Act4.2
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1010,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 2,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1241,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1246,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1247,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1248,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 1429,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2307,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2308,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2445,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2448,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2449,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2450,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 2451,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act42
            });
            drops.Add(new DropDTO
            {
                VNum = 5986,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act42
            });


            // Act5
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 1872,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 1873,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 1874,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2351,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 2379,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act51
            });

            // Act5.2
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2379,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 2380,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act52
            });

            // Act6.1 Angel
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2446,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2806,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2807,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2813,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2815,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2816,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2818,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 2819,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                VNum = 5880,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61A
            });

            // Act6.1 Demon
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2446,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2806,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2807,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2813,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2815,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2816,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2818,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 2819,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                VNum = 5881,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act61D
            });

            // Act6.2
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1191,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1192,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1193,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 1194,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2452,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2453,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2454,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2455,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 2456,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Act62
            });

            // Comet plain
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.CometPlain
            });

            // Mine1
            drops.Add(new DropDTO
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Mine1
            });
            drops.Add(new DropDTO
            {
                VNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Mine1
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 11000,
                MapTypeId = (short) MapTypeEnum.Mine1
            });

            // Mine2
            drops.Add(new DropDTO
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 11000,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Mine2
            });

            // MeadownOfMine
            drops.Add(new DropDTO
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 10000,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 2016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 2023,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 2024,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 2028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.MeadowOfMine
            });

            // SunnyPlain
            drops.Add(new DropDTO
            {
                VNum = 1003,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1006,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.SunnyPlain
            });

            // Fernon
            drops.Add(new DropDTO
            {
                VNum = 1003,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 1006,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 9000,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Fernon
            });

            // FernonF
            drops.Add(new DropDTO
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 9000,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.FernonF
            });

            // Cliff
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeEnum.Cliff
            });

            // LandOfTheDead
            drops.Add(new DropDTO
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1015,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1019,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1020,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1021,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1022,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 1211,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeEnum.LandOfTheDead
            });


            IEnumerable<DropDTO> dropDtos = drops;
            DAOFactory.DropDAO.InsertOrUpdate(dropDtos);
        }
    }
}