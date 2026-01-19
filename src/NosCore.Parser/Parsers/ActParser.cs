//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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

    public class ActParser(IDao<ActDto, byte> actDao, IDao<ActPartDto, byte> actDescDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly string _fileQuestDat = $"{Path.DirectorySeparatorChar}act_desc.dat";

        public async Task ImportActAsync(string folder)
        {
            var acts = new List<ActDto>();
            var actParts = new List<ActPartDto>();
            using (var stream = new StreamReader(folder + _fileQuestDat, Encoding.Default))
            {
                string? line;
                while ((line = await stream.ReadLineAsync()) != null)
                {
                    var splitted = line.Split(' ', '\t');
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

            await actDao.TryInsertOrUpdateAsync(acts);
            await actDescDao.TryInsertOrUpdateAsync(actParts);
            logger.Information(logLanguage[LogLanguageKey.ACTS_PARTS_PARSED], actParts.Count);
        }
    }
}
