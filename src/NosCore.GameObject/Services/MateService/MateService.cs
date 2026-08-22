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
    /// <inheritdoc cref="IMateService" />
    public class MateService(IDao<MateDto, long> mateDao, List<NpcMonsterDto> npcMonsters,
        IIdService<Mate> mateIdService, ILogger<MateService> logger) : IMateService
    {
        public Task<List<Mate>> LoadAsync(long characterId)
        {
            var rows = mateDao.Where(s => s.CharacterId == characterId)?.ToList() ?? new List<MateDto>();
            var mates = new List<Mate>();

            // Ordering by the stored id, not by whatever the database hands back: the slot a mate
            // occupies is what the client uses to address it, so it has to be the same on every
            // login or the player's pets would swap places between sessions.
            foreach (var row in rows.OrderBy(s => s.MateId))
            {
                var npcMonster = npcMonsters.Find(o => o.NpcMonsterVNum == row.VNum);
                if (npcMonster == null)
                {
                    // A row pointing at a creature the server does not know about. Skipping it
                    // loses the pet for this session but keeps the row, which is the recoverable
                    // half of a bad choice; sending it would mean a packet with no name in it.
                    logger.LogWarning("Mate {MateId} refers to unknown NpcMonster {VNum} and was skipped",
                        row.MateId, row.VNum);
                    continue;
                }

                var mate = row.Adapt<Mate>();
                mate.NpcMonster = npcMonster;
                mate.MateTransportId = mateIdService.GetNextId();
                mates.Add(mate);
            }

            // Pets and partners are numbered separately, each from zero — the capture shows
            // sc_p slots 0..7 next to sc_n slots 0..1 in one login burst.
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

        /// <summary>
        /// The packets that tell the client which mates the character owns, in the order the
        /// capture shows them: pets and partners interleaved by nothing in particular, each
        /// carrying its own slot number.
        /// </summary>
        public static IEnumerable<NosCore.Packets.Interfaces.IPacket> GenerateScPackets(
            IEnumerable<Mate> mates, RegionType language)
        {
            return mates.Select(mate => mate.MateType == MateType.Pet
                ? (NosCore.Packets.Interfaces.IPacket)mate.GenerateScp(language)
                : mate.GenerateScn(language));
        }
    }
}
