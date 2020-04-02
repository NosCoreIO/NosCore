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
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class ScriptParser
    {
        //script {ScriptId}	
        //{ScriptStepId}	{StepType} {Argument}
        private readonly string FileCardDat = $"{Path.DirectorySeparatorChar}tutorial.dat";
        private readonly IGenericDao<ScriptDto> _scriptDao;
        private readonly ILogger _logger;

        public ScriptParser(IGenericDao<ScriptDto> scriptDao, ILogger logger)
        {
            _logger = logger;
            _scriptDao = scriptDao;
        }

        public void InsertScripts(string folder)
        {
            var allScripts = _scriptDao.LoadAll().ToList();
            using var stream = new StreamReader(folder + FileCardDat, Encoding.Default);
            var scripts = new List<ScriptDto>();
            string? line;
            byte scriptId = 0;
            while ((line = stream.ReadLine()) != null)
            {
                var split = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (line.StartsWith("#"))
                {
                    continue;
                }
                if (split.Length > 1 && split[0] == "script")
                {
                    scriptId = Convert.ToByte(split[1]);
                }
                else if (split.Length > 2 && !allScripts.Any(s => s.ScriptId == scriptId && s.ScriptStepId == Convert.ToInt16(split[0])))
                {
                    var canParse = short.TryParse(split[2], out var argument1);
                    var stringArgument = !canParse ? split[2] : null;
                    scripts.Add(new ScriptDto()
                    {
                        Id = Guid.NewGuid(),
                        ScriptStepId = Convert.ToInt16(split[0]),
                        StepType = split[1],
                        StringArgument = stringArgument,
                        Argument1 = canParse ? argument1 : (short?)null,
                        Argument2 = split.Length > 3 && short.TryParse(split[3], out var argument2) ? argument2 : (short?)null,
                        Argument3 = split.Length > 4 && short.TryParse(split[4], out var argument3) ? argument3 : (short?)null,
                        ScriptId = scriptId
                    });
                }
            }

            _scriptDao.InsertOrUpdate(scripts);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SCRIPTS_PARSED), scripts.Count);
        }
    }
}