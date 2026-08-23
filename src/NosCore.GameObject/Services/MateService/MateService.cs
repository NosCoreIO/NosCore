//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Logging;
using NosCore.Core.Services.IdService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    public class MateService(IDao<MateDto, long> mateDao, List<NpcMonsterDto> npcMonsters,
        IIdService<Mate> mateIdService, ILogger<MateService> logger) : IMateService
    {
        public Task<List<Mate>> LoadAsync(long characterId)
        {
            var rows = mateDao.Where(s => s.CharacterId == characterId)?.ToList() ?? new List<MateDto>();
            var mates = new List<Mate>();

            foreach (var row in rows.OrderBy(s => s.MateId))
            {
                var npcMonster = npcMonsters.Find(o => o.NpcMonsterVNum == row.VNum);
                if (npcMonster == null)
                {
                    logger.LogWarning("Mate {MateId} refers to unknown NpcMonster {VNum} and was skipped",
                        row.MateId, row.VNum);
                    continue;
                }

                var mate = row.Adapt<Mate>();
                mate.NpcMonster = npcMonster;
                mate.MateTransportId = mateIdService.GetNextId();
                mates.Add(mate);
            }

            foreach (var group in mates.GroupBy(s => s.MateType))
            {
                byte slot = 0;
                foreach (var mate in group)
                {
                    mate.PetSlot = slot++;
                }
            }

            return Task.FromResult(mates);
        }

        public async Task SaveAsync(IEnumerable<Mate> mates)
        {
            foreach (var mate in mates)
            {
                await mateDao.TryInsertOrUpdateAsync(mate.Adapt<MateDto>()).ConfigureAwait(false);
            }
        }

        public static IEnumerable<NosCore.Packets.Interfaces.IPacket> GenerateScPackets(
            IEnumerable<Mate> mates, RegionType language)
        {
            return mates.Select(mate => mate.MateType == MateType.Pet
                ? (NosCore.Packets.Interfaces.IPacket)mate.GenerateScp(language)
                : mate.GenerateScn(language));
        }
    }
}
