//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Mates;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class PetSkillBarService(IClock clock, ISkillCatalog skills, INpcCombatCatalog catalog)
        : IPetSkillBarService, ISingletonService
    {
        private const int Slots = 3;


        public IReadOnlyList<SkillDto> Skills(Mate pet) =>
            pet.MateType != MateType.Pet
                ? []
                : catalog.GetSkills(pet.VNum)
                    .OrderBy(s => s.NpcMonsterSkillId)
                    .Select(s => skills.GetSkill(s.SkillVNum))
                    .OfType<SkillDto>()
                    .Where(s => s.IsMateSkill())
                    .Take(Slots)
                    .ToList();

        public async Task RefreshAsync(PlayerComponentBundle owner)
        {
            var pet = owner.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Pet && s.IsTeamMember && s.Entity != null);
            var bar = pet == null ? [] : Skills(pet);
            await owner.SendPacketAsync(bar.GeneratePetski());
            if (pet?.Entity is not { } entity)
            {
                return;
            }

            // The slots still cooling down from before the bar came back, which the map announces again when ready.
            var now = clock.GetCurrentInstant();
            var left = new int[Slots];
            for (var slot = 0; slot < bar.Count; slot++)
            {
                if (entity.NextUsableAt.TryGetValue(bar[slot].SkillVNum, out var readyAt) && readyAt > now)
                {
                    entity.PendingResets[slot] = readyAt;
                    left[slot] = (int)((readyAt - now).TotalMilliseconds / 100);
                }
            }

            await owner.SendPacketAsync(new PetCool2Packet { Cooldown1 = left[0], Cooldown2 = left[1], Cooldown3 = left[2] });
        }
    }
}
