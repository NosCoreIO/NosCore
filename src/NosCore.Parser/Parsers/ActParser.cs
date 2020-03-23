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
using System.Text;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using Serilog;

namespace NosCore.Parser.Parsers
{
    //# Act Data
    //#===================================
    //Data {ActPartId} {ActId} {} 10
    //Data 2 1 2 6
    //Data 3 1 3 8
    //Data 4 1 4 10
    //Data 5 1 5 6
    //Data 6 1 6 6
    //Data 7 2 1 3
    //Data 8 2 2 2
    //Data 9 2 3 3
    //Data 10 2 4 3
    //Data 11 2 5 1
    //Data 12 2 6 1
    //Data 13 3 1 2
    //Data 14 3 2 3
    //Data 15 3 3 2
    //Data 16 3 4 3
    //Data 17 3 5 3
    //Data 18 3 6 2
    //Data 19 4 1 1
    //Data 20 5 1 1
    //end
    //#==================================#
    //# Title
    //A   {ActId}	{Name}
    //~

    public class ActParser
    {
        private readonly string _fileQuestDat = $"{Path.DirectorySeparatorChar}act_desc.dat";
        private readonly ILogger _logger;
        private readonly IGenericDao<ActDto> _actDao;
        private readonly IGenericDao<ActPartDto> _actDescDao;


        public ActParser(IGenericDao<ActDto> actDao, IGenericDao<ActPartDto> actDescDao, ILogger logger)
        {
            _logger = logger;
            _actDao = actDao;
            _actDescDao = actDescDao;
        }

        public void ImportAct(string folder)
        {
            var acts = new List<ActDto>();
            var actParts = new List<ActPartDto>();
            using (var stream = new StreamReader(folder + _fileQuestDat, Encoding.Default))
            {
                string? line;
                while ((line = stream.ReadLine()) != null)
                {
                    var splitted = line.Split(' ','\t');
                    switch (splitted.Length)
                    {
                        case 3 when splitted[0] == "A":
                            acts.Add(new ActDto
                            {
                                TitleI18NKey = splitted[2],
                                ActId = Convert.ToByte(splitted[1]),
                                Scene = (byte)(39 + Convert.ToByte(splitted[1]))
                            });
                            continue;
                        case 5 when splitted[0] == "Data":
                            actParts.Add(new ActPartDto
                            {
                                ActPartId = Convert.ToByte(splitted[1]),
                                ActPartNumber = Convert.ToByte(splitted[3]),
                                ActId = Convert.ToByte(splitted[2]),
                                MaxTs = Convert.ToByte(splitted[4]),
                            });
                            continue;
                    }
                }
            }

            _actDao.InsertOrUpdate(acts);
            _actDescDao.InsertOrUpdate(actParts);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ACTS_PARTS_PARSED), actParts.Count);
        }
    }
}