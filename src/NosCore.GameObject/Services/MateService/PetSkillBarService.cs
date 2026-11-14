//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public sealed class PetSkillBarService(List<SkillDto> skills, INpcCombatCatalog catalog)
        : IPetSkillBarService, ISingletonService
    {
        private const int Slots = 3;
        private const short EverySlot = -1;

        private readonly Dictionary<short, SkillDto> _skillsByVnum = skills.ToDictionary(s => s.SkillVNum);

        public IReadOnlyList<SkillDto> Skills(Mate pet) =>
            pet.MateType != MateType.Pet
                ? []
                : catalog.GetSkills(pet.VNum)
                    .OrderBy(s => s.NpcMonsterSkillId)
                    .Select(s => _skillsByVnum.GetValueOrDefault(s.SkillVNum))
                    .OfType<SkillDto>()
                    .Where(s => s.IsMateSkill())
                    .Take(Slots)
                    .ToList();

        public async Task RefreshAsync(PlayerComponentBundle owner)
        {
            var pet = owner.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Pet && s.IsTeamMember && s.Entity != null);
            var bar = pet == null ? [] : Skills(pet);
            await owner.SendPacketAsync(bar.GeneratePetski());

            // The client keeps a slot greyed out while its bar is away, and a pet's cooldowns end with its
            // entity: a bar shown for a pet with nothing cooling down has to be told so.
            if (bar.Count > 0 && pet?.Entity is { PendingResets.IsEmpty: true })
            {
                await owner.SendPacketAsync(new PetsrPacket { Slot = EverySlot });
            }
        }
    }
}
