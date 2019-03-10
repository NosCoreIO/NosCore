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

using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Parser.Parsers
{
    public class DropParser
    {
        private readonly IGenericDao<DropDto> _dropDao = new GenericDao<Database.Entities.Drop, DropDto>();
        public void InsertDrop()
        {
            var drops = new List<DropDto>();
            // Act 1
            drops.Add(new DropDto
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act1
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 12000,
                MapTypeId = (short) MapTypeType.Act1
            });
            drops.Add(new DropDto
            {
                VNum = 2015,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act1
            });
            drops.Add(new DropDto
            {
                VNum = 2016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act1
            });
            drops.Add(new DropDto
            {
                VNum = 2023,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act1
            });
            drops.Add(new DropDto
            {
                VNum = 2024,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act1
            });
            drops.Add(new DropDto
            {
                VNum = 2028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act1
            });

            // Act2
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Act2
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 80,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 80,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 80,
                MapTypeId = (short) MapTypeType.Oasis
            });

            // Act3
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 4000,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 350,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act3
            });

            // Act3.2 (Midgard)
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 20,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 20,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 60,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 40,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 60,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 40,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2600,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 2605,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 5857,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act32
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act32
            });


            // Act 3.4 Oasis 
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });
            drops.Add(new DropDto
            {
                VNum = 5999,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Oasis
            });

            // Act4
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1010,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 2,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1241,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1246,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1247,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1248,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 1429,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 2307,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act4
            });
            drops.Add(new DropDto
            {
                VNum = 2308,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act4
            });

            //Act4.2
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1010,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 2,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1241,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1246,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1247,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1248,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 1429,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2307,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2308,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2445,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2448,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2449,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2450,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 2451,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act42
            });
            drops.Add(new DropDto
            {
                VNum = 5986,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act42
            });


            // Act5
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 1872,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 1873,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 1874,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2351,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 2379,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act51
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act51
            });

            // Act5.2
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2379,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 2380,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeType.Act52
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act52
            });

            // Act6.1 Angel
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2446,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2806,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2807,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2813,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2815,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2816,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2818,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 2819,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act61A
            });
            drops.Add(new DropDto
            {
                VNum = 5880,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61A
            });

            // Act6.1 Demon
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2446,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2806,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2807,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2813,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2815,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2816,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2818,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 2819,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act61D
            });
            drops.Add(new DropDto
            {
                VNum = 5881,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act61D
            });

            // Act6.2
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act61
            });
            drops.Add(new DropDto
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1191,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1192,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1193,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 1194,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2452,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2453,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2454,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2455,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 2456,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short) MapTypeType.Act62
            });
            drops.Add(new DropDto
            {
                VNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Act62
            });

            // Comet plain
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.CometPlain
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.CometPlain
            });

            // Mine1
            drops.Add(new DropDto
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Mine1
            });
            drops.Add(new DropDto
            {
                VNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Mine1
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 11000,
                MapTypeId = (short) MapTypeType.Mine1
            });

            // Mine2
            drops.Add(new DropDto
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 11000,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.Mine2
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Mine2
            });

            // MeadownOfMine
            drops.Add(new DropDto
            {
                VNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 10000,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 2016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 2023,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 2024,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 2028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });
            drops.Add(new DropDto
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.MeadowOfMine
            });

            // SunnyPlain
            drops.Add(new DropDto
            {
                VNum = 1003,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1006,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.SunnyPlain
            });

            // Fernon
            drops.Add(new DropDto
            {
                VNum = 1003,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 1006,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 9000,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Fernon
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Fernon
            });

            // FernonF
            drops.Add(new DropDto
            {
                VNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 9000,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.FernonF
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.FernonF
            });

            // Cliff
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeType.Cliff
            });
            drops.Add(new DropDto
            {
                VNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Cliff
            });
            drops.Add(new DropDto
            {
                VNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Cliff
            });
            drops.Add(new DropDto
            {
                VNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Cliff
            });
            drops.Add(new DropDto
            {
                VNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Cliff
            });
            drops.Add(new DropDto
            {
                VNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Cliff
            });
            drops.Add(new DropDto
            {
                VNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.Cliff
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short) MapTypeType.Cliff
            });

            // LandOfTheDead
            drops.Add(new DropDto
            {
                VNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1015,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1019,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1020,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1021,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1022,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 1211,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });
            drops.Add(new DropDto
            {
                VNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short) MapTypeType.LandOfTheDead
            });


            IEnumerable<DropDto> dropDtos = drops;
            _dropDao.InsertOrUpdate(dropDtos);
        }
    }
}