//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    // A refiner's rows form two draws: its Cella amount, then its extra item, where a row with no
    // amount is the share that gives none.
    public class RefinerParser(IDao<ItemDto, short> itemDao,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao, ILogger<RefinerParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public const short CellaRoll = 0;
        public const short ExtraRoll = 1;

        public async Task InsertRefinerRollsAsync()
        {
            var items = itemDao.LoadAll().ToDictionary(i => i.VNum);
            var nextId = (short)((rollGeneratedItemDao.LoadAll().Select(r => (int)r.RollGeneratedItemId).DefaultIfEmpty().Max()) + 1);
            var rolls = new List<RollGeneratedItemDto>();
            var refiners = 0;
            foreach (var refiner in items.Values.Where(i => i is { ItemType: ItemType.Main, Effect: ItemEffectType.Refiner }).OrderBy(i => i.VNum))
            {
                if (!Refiners.Extras.TryGetValue((RefinerType)refiner.EffectValue, out var extras)
                    || !items.ContainsKey(Refiners.CellaPowder) || extras.Any(e => !items.ContainsKey(e.VNum)))
                {
                    continue;
                }

                refiners++;
                rolls.AddRange(Refiners.CellaAmounts.Select(amount => Row(refiner.VNum, CellaRoll, Refiners.CellaPowder, amount, 1)));
                rolls.AddRange(extras.Select(e => Row(refiner.VNum, ExtraRoll, e.VNum, 1, e.Chance)));
                rolls.Add(Row(refiner.VNum, ExtraRoll, Refiners.CellaPowder, 0, (short)(Refiners.ChanceScale - extras.Sum(e => e.Chance))));
            }

            foreach (var roll in rolls)
            {
                roll.RollGeneratedItemId = nextId++;
            }

            await rollGeneratedItemDao.TryInsertOrUpdateAsync(rolls).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.REFINERS_PARSED], refiners, rolls.Count);
        }

        private static RollGeneratedItemDto Row(short refiner, short group, short vnum, short amount, short probability) => new()
        {
            OriginalItemVNum = refiner,
            RollGroup = group,
            ItemGeneratedVNum = vnum,
            ItemGeneratedAmount = amount,
            Probability = probability,
        };
    }
}
