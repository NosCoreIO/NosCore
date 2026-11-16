//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapTypeParser(IDao<MapTypeDto, MapTypeType> dropDao, ILogger<MapTypeParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        internal async Task InsertMapTypesAsync()
        {
            var mts = new List<MapTypeDto> { new()
                {
                    MapTypeId = MapTypeType.Act1,
                    MapTypeName = "Act1",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Act2,
                    MapTypeName = "Act2",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Act3,
                    MapTypeName = "Act3",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Act4,
                    MapTypeName = "Act4",
                    PotionDelay = 5000
                },
                new()
                {
                    MapTypeId = MapTypeType.Act51,
                    MapTypeName = "Act5.1",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct5,
                    ReturnMapTypeId = RespawnType.ReturnAct5
                },
                new()
                {
                    MapTypeId = MapTypeType.Act52,
                    MapTypeName = "Act5.2",
                    PotionDelay = 300,
                    ZoneCardId = 339,
                    RespawnMapTypeId = RespawnType.DefaultAct5,
                    ReturnMapTypeId = RespawnType.ReturnAct5
                },
                new()
                {
                    MapTypeId = MapTypeType.Act61,
                    MapTypeName = "Act6.1",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct6,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Act62,
                    MapTypeName = "Act6.2",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct6,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Act61A,
                    MapTypeName = "Act6.1a", // angel camp
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct6,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Act61D,
                    MapTypeName = "Act6.1d", // demon camp
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct6,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.CometPlain,
                    MapTypeName = "CometPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Mine1,
                    MapTypeName = "Mine1",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Mine2,
                    MapTypeName = "Mine2",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.MeadowOfMine,
                    MapTypeName = "MeadownOfPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.SunnyPlain,
                    MapTypeName = "SunnyPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Fernon,
                    MapTypeName = "Fernon",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.FernonF,
                    MapTypeName = "FernonF",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.Cliff,
                    MapTypeName = "Cliff",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultAct1,
                    ReturnMapTypeId = RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = MapTypeType.LandOfTheDead,
                    MapTypeName = "LandOfTheDead",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = MapTypeType.Act32,
                    MapTypeName = "Act 3.2",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = MapTypeType.CleftOfDarkness,
                    MapTypeName = "Cleft of Darkness",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = MapTypeType.CitadelAngel,
                    MapTypeName = "AngelCitadel",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = MapTypeType.CitadelDemon,
                    MapTypeName = "DemonCitadel",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = MapTypeType.Oasis,
                    MapTypeName = "Oasis",
                    PotionDelay = 300,
                    RespawnMapTypeId = RespawnType.DefaultOasis,
                    ReturnMapTypeId = RespawnType.DefaultOasis
                },
                new()
                {
                    MapTypeId = MapTypeType.Act42,
                    MapTypeName = "Act42",
                    PotionDelay = 5000
                }
            };
            await dropDao.TryInsertOrUpdateAsync(mts).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.MAPTYPES_PARSED]);
        }
    }
}
