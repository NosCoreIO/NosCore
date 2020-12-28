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

using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class RespawnMapTypeParser
    {
        private readonly ILogger _logger;

        private readonly IDao<RespawnMapTypeDto, long> _respawnMapTypeDao;

        public RespawnMapTypeParser(IDao<RespawnMapTypeDto, long> respawnMapTypeDao, ILogger logger)
        {
            _respawnMapTypeDao = respawnMapTypeDao;
            _logger = logger;
        }

        internal async Task InsertRespawnMapTypeAsync()
        {
            var respawnmaptypemaps = new List<RespawnMapTypeDto>
            {
                new RespawnMapTypeDto
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    MapId = 1,
                    DefaultX = 80,
                    DefaultY = 116,
                    Name = "Default"
                },
                new RespawnMapTypeDto
                {
                    RespawnMapTypeId = (long) RespawnType.ReturnAct1,
                    MapId = 0,
                    DefaultX = 0,
                    DefaultY = 0,
                    Name = "Return"
                },
                new RespawnMapTypeDto
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                    MapId = 170,
                    DefaultX = 86,
                    DefaultY = 48,
                    Name = "DefaultAct5"
                },
                new RespawnMapTypeDto
                {
                    RespawnMapTypeId = (long) RespawnType.ReturnAct5,
                    MapId = 0,
                    DefaultX = 0,
                    DefaultY = 0,
                    Name = "ReturnAct5"
                },
                new RespawnMapTypeDto
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    MapId = 228,
                    DefaultX = 72,
                    DefaultY = 102,
                    Name = "DefaultAct6"
                },
                new RespawnMapTypeDto
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultAct62,
                    MapId = 228,
                    DefaultX = 72,
                    DefaultY = 102,
                    Name = "DefaultAct62"
                },
                new RespawnMapTypeDto
                {
                    RespawnMapTypeId = (long) RespawnType.DefaultOasis,
                    MapId = 261,
                    DefaultX = 66,
                    DefaultY = 70,
                    Name = "DefaultOasis"
                }
            };
            await _respawnMapTypeDao.TryInsertOrUpdateAsync(respawnmaptypemaps).ConfigureAwait(false);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.RESPAWNTYPE_PARSED),
                respawnmaptypemaps.Count());
        }
    }
}