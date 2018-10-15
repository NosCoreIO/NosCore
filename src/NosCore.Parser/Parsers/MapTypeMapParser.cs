//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
    public class MapTypeMapParser
    {
        internal void InsertMapTypeMaps()
        {
            var maptypemaps = new List<MapTypeMapDTO>();
            short mapTypeId = 1;
            for (var i = 1; i < 300; i++)
            {
                var objectset = false;
                if (i < 3 || (i > 48 && i < 53) || (i > 67 && i < 76) || i == 102 || (i > 103 && i < 105)
                    || (i > 144 && i < 149))
                {
                    // "act1"
                    mapTypeId = (short) MapTypeEnum.Act1;
                    objectset = true;
                }
                else if ((i > 19 && i < 34) || (i > 52 && i < 68) || (i > 84 && i < 101))
                {
                    // "act2"
                    mapTypeId = (short) MapTypeEnum.Act2;
                    objectset = true;
                }
                else if ((i > 40 && i < 45) || (i > 45 && i < 48) || (i > 99 && i < 102) || (i > 104 && i < 128))
                {
                    // "act3"
                    mapTypeId = (short) MapTypeEnum.Act3;
                    objectset = true;
                }
                else if (i == 260)
                {
                    // "act3.2"
                    mapTypeId = (short) MapTypeEnum.Act32;
                    objectset = true;
                }
                else if ((i > 129 && i <= 134) || i == 135 || i == 137 || i == 139 || i == 141 || (i > 150 && i < 153))
                {
                    // "act4"
                    mapTypeId = (short) MapTypeEnum.Act4;
                    objectset = true;
                }
                else if (i == 153)
                {
                    // "act4.2"
                    mapTypeId = (short) MapTypeEnum.Act42;
                    objectset = true;
                }
                else if (i > 169 && i < 205)
                {
                    // "act5.1"
                    mapTypeId = (short) MapTypeEnum.Act51;
                    objectset = true;
                }
                else if (i > 204 && i < 221)
                {
                    // "act5.2"
                    mapTypeId = (short) MapTypeEnum.Act52;
                    objectset = true;
                }
                else if (i > 228 && i < 233)
                {
                    // "act6.1a"
                    mapTypeId = (short) MapTypeEnum.Act61;
                    objectset = true;
                }
                else if (i > 232 && i < 238)
                {
                    // "act6.1d"
                    mapTypeId = (short) MapTypeEnum.Act61;
                    objectset = true;
                }
                else if ((i > 239 && i < 251) || i == 299)
                {
                    // "act6.2"
                    mapTypeId = (short) MapTypeEnum.Act62;
                    objectset = true;
                }
                else if ((i > 260 && i < 264) || (i > 2614 && i < 2621))
                {
                    // "Oasis"
                    mapTypeId = (short) MapTypeEnum.Oasis;
                    objectset = true;
                }
                else if (i == 103)
                {
                    // "Comet plain"
                    mapTypeId = (short) MapTypeEnum.CometPlain;
                    objectset = true;
                }
                else if (i == 6)
                {
                    // "Mine1"
                    mapTypeId = (short) MapTypeEnum.Mine1;
                    objectset = true;
                }
                else if (i > 6 && i < 9)
                {
                    // "Mine2"
                    mapTypeId = (short) MapTypeEnum.Mine2;
                    objectset = true;
                }
                else if (i == 3)
                {
                    // "Meadown of mine"
                    mapTypeId = (short) MapTypeEnum.MeadowOfMine;
                    objectset = true;
                }
                else if (i == 4)
                {
                    // "Sunny plain"
                    mapTypeId = (short) MapTypeEnum.SunnyPlain;
                    objectset = true;
                }
                else if (i == 5)
                {
                    // "Fernon"
                    mapTypeId = (short) MapTypeEnum.Fernon;
                    objectset = true;
                }
                else if ((i > 9 && i < 19) || (i > 79 && i < 85))
                {
                    // "FernonF"
                    mapTypeId = (short) MapTypeEnum.FernonF;
                    objectset = true;
                }
                else if (i > 75 && i < 79)
                {
                    // "Cliff"
                    mapTypeId = (short) MapTypeEnum.Cliff;
                    objectset = true;
                }
                else if (i == 150)
                {
                    // "Land of the dead"
                    mapTypeId = (short) MapTypeEnum.LandOfTheDead;
                    objectset = true;
                }
                else if (i == 138)
                {
                    // "Cleft of Darkness"
                    mapTypeId = (short) MapTypeEnum.CleftOfDarkness;
                    objectset = true;
                }
                else if (i == 130)
                {
                    // "Citadel"
                    mapTypeId = (short) MapTypeEnum.CitadelAngel;
                    objectset = true;
                }
                else if (i == 131)
                {
                    mapTypeId = (short) MapTypeEnum.CitadelDemon;
                    objectset = true;
                }

                // add "act6.1a" and "act6.1d" when ids found
                var i1 = (short) i;
                var id = mapTypeId;
                if (objectset && DAOFactory.MapDAO.FirstOrDefault(s => s.MapId.Equals((short) i)) != null
                    && DAOFactory.MapTypeMapDAO.FirstOrDefault(s => s.MapId.Equals(i1) && s.MapTypeId.Equals(id)) ==
                    null)
                {
                    maptypemaps.Add(new MapTypeMapDTO {MapId = (short) i, MapTypeId = mapTypeId});
                }
            }

            IEnumerable<MapTypeMapDTO> mapDtos = maptypemaps;
            DAOFactory.MapTypeMapDAO.InsertOrUpdate(mapDtos);
        }
    }
}