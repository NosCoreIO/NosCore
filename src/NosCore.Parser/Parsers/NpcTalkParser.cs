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
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
    public class NpcTalkParser(IDao<NpcTalkDto, short> npcTalkDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly string _fileNpcTalkDat = $"{Path.DirectorySeparatorChar}npctalk.dat";


        public async Task ParseAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(NpcTalkDto.DialogId), chunk => Convert.ToInt16(chunk["%"][0][1])},
                {nameof(NpcTalkDto.NameI18NKey), chunk => chunk["t"][0][1]},
            };

            var genericParser = new GenericParser<NpcTalkDto>(folder + _fileNpcTalkDat,
                "%", 0, actionList, logger, logLanguage);
            var npcTalks = (await genericParser.GetDtosAsync(" ").ConfigureAwait(false)).ToList();
            npcTalks.Add(new NpcTalkDto { DialogId = 99, NameI18NKey = "" });
            await npcTalkDao.TryInsertOrUpdateAsync(npcTalks).ConfigureAwait(false);

            logger.Information(logLanguage[LogLanguageKey.NPCTALKS_PARSED], npcTalks.Count);
        }

    }
}