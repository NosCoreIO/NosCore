//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Algorithm.MateExperienceService;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Networking;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class MateProgressionService(IMateExperienceService mateExperienceService) : IMateProgressionService
    {
        public async Task AddExperienceAsync(PlayerComponentBundle owner, long experience)
        {
            if (experience <= 0)
            {
                return;
            }

            foreach (var mate in owner.Mates.Values.Where(s => s.IsTeamMember))
            {
                var leveledUp = false;
                mate.XpLoad = XpLoad(mate);
                mate.Experience += experience;
                // A mate stays below its owner: the client refuses a partner of the same level.
                while (mate.XpLoad > 0 && mate.Experience >= mate.XpLoad && mate.Level + 1 < owner.Level)
                {
                    mate.Experience -= mate.XpLoad;
                    mate.Level++;
                    mate.XpLoad = XpLoad(mate);
                    leveledUp = true;
                }

                mate.Experience = Math.Min(mate.Experience, mate.XpLoad);

                if (leveledUp)
                {
                    mate.Hp = mate.MaxHp;
                    mate.Mp = mate.MaxMp;
                    if (mate.Entity is { } entity)
                    {
                        entity.Level = mate.Level;
                        entity.Hp = mate.Hp;
                        entity.Mp = mate.Mp;
                        await owner.MapInstance.SendPacketAsync(entity.GenerateEff(6));
                        await owner.MapInstance.SendPacketAsync(entity.GenerateEff(198));
                    }
                }

                await owner.SendPacketAsync(mate.MateType == MateType.Pet
                    ? mate.GenerateScp(owner.AccountLanguage)
                    : mate.GenerateScn(owner.AccountLanguage, owner.InventoryService));
            }
        }

        private long XpLoad(Mate mate) => mate.MateType == MateType.Pet
            ? mateExperienceService.GetPetExperience(mate.Level)
            : mateExperienceService.GetPartnerExperience(mate.Level);
    }
}
