//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers

{
    public class MapTypeMapParser(IDao<MapTypeMapDto, short> mapTypeMapDao, IDao<MapDto, short> mapDao)
    {
        internal Task InsertMapTypeMapsAsync()
        {
            var maptypemaps = new List<MapTypeMapDto>();
            short mapTypeId = 1;
            var mapsdb = mapDao.LoadAll().ToList();
            var maptypemapdb = mapTypeMapDao.LoadAll().ToList();
            for (var i = 1; i < 300; i++)
            {
                var objectset = false;
                if ((i < 3) || ((i > 48) && (i < 53)) || ((i > 67) && (i < 76)) || (i == 102) ||
                    ((i > 103) && (i < 105))
                    || ((i > 144) && (i < 149)))
                {
                    // "act1"
                    mapTypeId = (short)MapTypeType.Act1;
                    objectset = true;
                }
                else if (((i > 19) && (i < 34)) || ((i > 52) && (i < 68)) || ((i > 84) && (i < 101)))
                {
                    // "act2"
                    mapTypeId = (short)MapTypeType.Act2;
                    objectset = true;
                }
                else if (((i > 40) && (i < 45)) || ((i > 45) && (i < 48)) || ((i > 99) && (i < 102)) ||
                    ((i > 104) && (i < 128)))
                {
                    // "act3"
                    mapTypeId = (short)MapTypeType.Act3;
                    objectset = true;
                }
                else if (i == 260)
                {
                    // "act3.2"
                    mapTypeId = (short)MapTypeType.Act32;
                    objectset = true;
                }
                else if (((i > 129) && (i <= 134)) || (i == 135) || (i == 137) || (i == 139) || (i == 141) ||
                    ((i > 150) && (i < 153)))
                {
                    // "act4"
                    mapTypeId = (short)MapTypeType.Act4;
                    objectset = true;
                }
                else if (i == 153)
                {
                    // "act4.2"
                    mapTypeId = (short)MapTypeType.Act42;
                    objectset = true;
                }
                else if ((i > 169) && (i < 205))
                {
                    // "act5.1"
                    mapTypeId = (short)MapTypeType.Act51;
                    objectset = true;
                }
                else if ((i > 204) && (i < 221))
                {
                    // "act5.2"
                    mapTypeId = (short)MapTypeType.Act52;
                    objectset = true;
                }
                else if (((i > 228) && (i < 233)) || ((i > 232) && (i < 238)))
                {
                    mapTypeId = (short)MapTypeType.Act61;
                    objectset = true;
                }
                else if (((i > 239) && (i < 251)) || (i == 299))
                {
                    // "act6.2"
                    mapTypeId = (short)MapTypeType.Act62;
                    objectset = true;
                }
                else if (((i > 260) && (i < 264)) || ((i > 2614) && (i < 2621)))
                {
                    // "Oasis"
                    mapTypeId = (short)MapTypeType.Oasis;
                    objectset = true;
                }
                else if (i == 103)
                {
                    // "Comet plain"
                    mapTypeId = (short)MapTypeType.CometPlain;
                    objectset = true;
                }
                else if (i == 6)
                {
                    // "Mine1"
                    mapTypeId = (short)MapTypeType.Mine1;
                    objectset = true;
                }
                else if ((i > 6) && (i < 9))
                {
                    // "Mine2"
                    mapTypeId = (short)MapTypeType.Mine2;
                    objectset = true;
                }
                else if (i == 3)
                {
                    // "Meadown of mine"
                    mapTypeId = (short)MapTypeType.MeadowOfMine;
                    objectset = true;
                }
                else if (i == 4)
                {
                    // "Sunny plain"
                    mapTypeId = (short)MapTypeType.SunnyPlain;
                    objectset = true;
                }
                else if (i == 5)
                {
                    // "Fernon"
                    mapTypeId = (short)MapTypeType.Fernon;
                    objectset = true;
                }
                else if (((i > 9) && (i < 19)) || ((i > 79) && (i < 85)))
                {
                    // "FernonF"
                    mapTypeId = (short)MapTypeType.FernonF;
                    objectset = true;
                }
                else if ((i > 75) && (i < 79))
                {
                    // "Cliff"
                    mapTypeId = (short)MapTypeType.Cliff;
                    objectset = true;
                }
                else if (i == 150)
                {
                    // "Land of the dead"
                    mapTypeId = (short)MapTypeType.LandOfTheDead;
                    objectset = true;
                }
                else if (i == 138)
                {
                    // "Cleft of Darkness"
                    mapTypeId = (short)MapTypeType.CleftOfDarkness;
                    objectset = true;
                }
                else if (i == 130)
                {
                    // "Citadel"
                    mapTypeId = (short)MapTypeType.CitadelAngel;
                    objectset = true;
                }
                else if (i == 131)
                {
                    mapTypeId = (short)MapTypeType.CitadelDemon;
                    objectset = true;
                }

                // add "act6.1a" and "act6.1d" when ids found
                var i1 = (short)i;
                var id = mapTypeId;
                if (objectset && (mapsdb.FirstOrDefault(s => s.MapId == i) !=
                        null)
                    && (maptypemapdb
                            .FirstOrDefault(s => (s.MapId == i1) && (s.MapTypeId == id))
                        == null))
                {
                    maptypemaps.Add(new MapTypeMapDto { MapId = (short)i, MapTypeId = mapTypeId });
                }
            }

            IEnumerable<MapTypeMapDto> mapDtos = maptypemaps;
            return mapTypeMapDao.TryInsertOrUpdateAsync(mapDtos);
        }
    }
}
