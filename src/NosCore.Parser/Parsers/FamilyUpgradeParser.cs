//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // Which items the family manager sells only shows in its "n_inv ... {vnum}|{state}|{slot}" list;
    // what each one requires is in that item's DATA line.
    public class FamilyUpgradeParser(IDao<FamilyUpgradeDto, short> familyUpgradeDao, ILoggerFactory loggerFactory,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly ILogger<FamilyUpgradeParser> _logger = loggerFactory.CreateLogger<FamilyUpgradeParser>();
        private readonly string _itemDat = $"{Path.DirectorySeparatorChar}Item.dat";

        public FluentParserBuilder<FamilyUpgradeDto> BuildParser(string folder)
        {
            return FluentParserBuilder<FamilyUpgradeDto>.Create(folder + _itemDat, "END", 1)
                .Field(x => x.ItemVNum, "VNUM", 0, 2, s => Convert.ToInt16(s), "Item vnum")
                .Field(x => x.FamilyLevelMinimum, "DATA", 0, 2, s => Convert.ToByte(s), "Family level needed to buy it")
                .Field(x => x.UpgradeType, "DATA", 0, 4, s => (FamilyUpgradeType)Convert.ToByte(s), "Upgrade line")
                .Field(x => x.UpgradeLevel, "DATA", 0, 5, s => Convert.ToByte(s), "Position in its line, 0 first");
        }

        public async Task InsertFamilyUpgradesAsync(string folder, List<string[]> packetList)
        {
            var listed = packetList
                .Where(packet => packet.Length > 4 && packet[0] == "n_inv")
                .SelectMany(packet => packet.Skip(4))
                .Select(entry => entry.Split('|'))
                .Where(entry => entry.Length == 3 && short.TryParse(entry[0], out _))
                .Select(entry => short.Parse(entry[0]))
                .ToHashSet();
            if (listed.Count == 0)
            {
                _logger.LogInformation(logLanguage[LogLanguageKey.FAMILY_UPGRADES_PARSED], 0);
                return;
            }

            var parser = BuildParser(folder).Build(loggerFactory, logLanguage);
            var upgrades = (await parser.GetDtosAsync())
                .Where(s => listed.Contains(s.ItemVNum) && Enum.IsDefined(s.UpgradeType))
                .GroupBy(s => s.ItemVNum)
                .Select(s => s.First())
                .ToList();
            await familyUpgradeDao.TryInsertOrUpdateAsync(upgrades);
            _logger.LogInformation(logLanguage[LogLanguageKey.FAMILY_UPGRADES_PARSED], upgrades.Count);
        }
    }
}
