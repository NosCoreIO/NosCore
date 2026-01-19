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

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapTypeParser(IDao<MapTypeDto, short> dropDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        internal async Task InsertMapTypesAsync()
        {
            var mts = new List<MapTypeDto> { new()
                {
                    MapTypeId = (short) MapTypeType.Act1,
                    MapTypeName = "Act1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act2,
                    MapTypeName = "Act2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act3,
                    MapTypeName = "Act3",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act4,
                    MapTypeName = "Act4",
                    PotionDelay = 5000
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act51,
                    MapTypeName = "Act5.1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct5
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act52,
                    MapTypeName = "Act5.2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct5
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act61,
                    MapTypeName = "Act6.1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act62,
                    MapTypeName = "Act6.2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act61A,
                    MapTypeName = "Act6.1a", // angel camp
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act61D,
                    MapTypeName = "Act6.1d", // demon camp
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.CometPlain,
                    MapTypeName = "CometPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Mine1,
                    MapTypeName = "Mine1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Mine2,
                    MapTypeName = "Mine2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.MeadowOfMine,
                    MapTypeName = "MeadownOfPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.SunnyPlain,
                    MapTypeName = "SunnyPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Fernon,
                    MapTypeName = "Fernon",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.FernonF,
                    MapTypeName = "FernonF",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Cliff,
                    MapTypeName = "Cliff",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.LandOfTheDead,
                    MapTypeName = "LandOfTheDead",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act32,
                    MapTypeName = "Act 3.2",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.CleftOfDarkness,
                    MapTypeName = "Cleft of Darkness",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.CitadelAngel,
                    MapTypeName = "AngelCitadel",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.CitadelDemon,
                    MapTypeName = "DemonCitadel",
                    PotionDelay = 300
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Oasis,
                    MapTypeName = "Oasis",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultOasis,
                    ReturnMapTypeId = (long) RespawnType.DefaultOasis
                },
                new()
                {
                    MapTypeId = (short) MapTypeType.Act42,
                    MapTypeName = "Act42",
                    PotionDelay = 5000
                }
            };
            await dropDao.TryInsertOrUpdateAsync(mts);
            logger.Information(logLanguage[LogLanguageKey.MAPTYPES_PARSED]);
        }
    }
}