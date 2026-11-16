//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // Which items the family manager sells only shows in its shop's family list; what each one
    // requires is in that item's DATA line.
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

        public async Task InsertFamilyUpgradesAsync(string folder, IReadOnlyList<IPacket> packets)
        {
            var listed = packets.OfType<NInvPacket>()
                .SelectMany(packet => packet.FamilySkills ?? [])
                .OfType<NInvFamilySkillSubPacket>()
                .Select(entry => entry.SkillId)
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
            await familyUpgradeDao.TryInsertOrUpdateAsync(upgrades).OrThrowAsync();
            _logger.LogInformation(logLanguage[LogLanguageKey.FAMILY_UPGRADES_PARSED], upgrades.Count);
        }
    }
}
