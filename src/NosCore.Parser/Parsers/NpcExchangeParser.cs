//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//


using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class NpcExchangeParser(IDao<ItemDto, short> itemDao, IDao<CardDto, short> cardDao, IDao<NpcExchangeDto, short> npcExchangeDao,
        IDao<NpcExchangeMaterialDto, short> npcExchangeMaterialDao, ILogger<NpcExchangeParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertNpcExchangesAsync()
        {
            var items = itemDao.LoadAll().Select(s => s.VNum).ToHashSet();
            var cards = cardDao.LoadAll().Select(s => s.CardId).ToHashSet();
            var exchanges = new List<NpcExchangeDto>();
            var materials = new List<NpcExchangeMaterialDto>();
            short exchangeId = 0;
            short materialId = 0;
            foreach (var exchange in NpcExchanges.All)
            {
                exchangeId++;
                if (!items.Contains(exchange.ItemVNum) || exchange.Materials.Any(m => !items.Contains(m.ItemVNum))
                    || exchange.CardId is { } card && !cards.Contains(card))
                {
                    materialId += (short)exchange.Materials.Length;
                    continue;
                }

                exchanges.Add(new NpcExchangeDto
                {
                    NpcExchangeId = exchangeId,
                    Runner = exchange.Runner,
                    ItemVNum = exchange.ItemVNum,
                    Amount = exchange.Amount,
                    CardId = exchange.CardId,
                });
                materials.AddRange(exchange.Materials.Select(m => new NpcExchangeMaterialDto
                {
                    NpcExchangeMaterialId = ++materialId,
                    NpcExchangeId = exchangeId,
                    ItemVNum = m.ItemVNum,
                    Amount = m.Amount,
                }));
            }

            var materialIds = materials.Select(s => s.NpcExchangeMaterialId).ToHashSet();
            await npcExchangeMaterialDao.TryDeleteAsync(npcExchangeMaterialDao.LoadAll()
                .Where(s => !materialIds.Contains(s.NpcExchangeMaterialId)).Select(s => s.NpcExchangeMaterialId).ToList());
            var exchangeIds = exchanges.Select(s => s.NpcExchangeId).ToHashSet();
            await npcExchangeDao.TryDeleteAsync(npcExchangeDao.LoadAll()
                .Where(s => !exchangeIds.Contains(s.NpcExchangeId)).Select(s => s.NpcExchangeId).ToList());
            await npcExchangeDao.TryInsertOrUpdateAsync(exchanges);
            await npcExchangeMaterialDao.TryInsertOrUpdateAsync(materials);
            logger.LogInformation(logLanguage[LogLanguageKey.NPC_EXCHANGES_PARSED], exchanges.Count);
        }
    }
}
