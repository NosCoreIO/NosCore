//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using Mapster;
using Microsoft.Extensions.Logging;
using NosCore.Algorithm.MateExperienceService;
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
        IIdService<Mate> mateIdService, IMateExperienceService mateExperienceService,
        ILogger<MateService> logger) : IMateService
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
                mate.XpLoad = mate.MateType == MateType.Pet
                    ? mateExperienceService.GetPetExperience(mate.Level)
                    : mateExperienceService.GetPartnerExperience(mate.Level);
                mates.Add(mate);
            }

            foreach (var group in mates.GroupBy(s => s.MateType))
            {
                byte slot = 0;
                var alreadyOut = false;
                foreach (var mate in group)
                {
                    mate.PetSlot = slot++;

                    // A character keeps one pet and one partner out at a time. Two rows can
                    // claim the slot — two captures racing, or a database edited by hand — and
                    // the second would spawn on top of the first with no error anywhere. The
                    // reader decides, so a bad row costs a mate that stays in the list rather
                    // than a broken map.
                    if (!mate.IsTeamMember)
                    {
                        continue;
                    }

                    mate.IsTeamMember = !alreadyOut;
                    alreadyOut = true;
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
