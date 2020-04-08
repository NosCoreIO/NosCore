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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers
{
    //% {DialogId}
    //t {Title}
    //s {ShopId}
    //c<list>
    //c<end>
    //b zts17695e  . n_talk 11 0&@
    //b zts3677e  . shopping 3 0
    //b zts7087e  . n_run 14 0
    //b zts3678e  . n_talk 1 0
    //b zts16037e  . n_talk 8 0&@
    //b zts16038e  . n_talk 7 0&@
    //b zts4759e  . n_run 3000 0
    //b zts4760e  . n_talk 100 0
    public class NpcTalkParser
    {
        private readonly string _fileNpcTalkDat = $"{Path.DirectorySeparatorChar}npctalk.dat";
        private readonly ILogger _logger;
        private readonly IDao<NpcTalkDto, short> _npcTalkDao;

        public NpcTalkParser(IDao<NpcTalkDto, short> npcTalkDao, ILogger logger)
        {
            _logger = logger;
            _npcTalkDao = npcTalkDao;
        }


        public async Task ParseAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(NpcTalkDto.DialogId), chunk => Convert.ToInt16(chunk["%"][0][1])},
                {nameof(NpcTalkDto.NameI18NKey), chunk => chunk["t"][0][1]},
            };

            var genericParser = new GenericParser<NpcTalkDto>(folder + _fileNpcTalkDat,
                "%", 0, actionList, _logger);
            var npcTalks = genericParser.GetDtos(" ").ToList();
            npcTalks.Add(new NpcTalkDto { DialogId = 99, NameI18NKey = "" });
            await _npcTalkDao.TryInsertOrUpdateAsync(npcTalks).ConfigureAwait(false);

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NPCTALKS_PARSED), npcTalks.Count);
        }

    }
}