using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public class SkillService(IDao<CharacterSkillDto, Guid> characterSkillDao, List<SkillDto> skills) : ISkillService
    {
        public async Task LoadSkill(ICharacterEntity character)
        {
            var characterSkills = characterSkillDao.Where(x => x.CharacterId == character.VisualId).Adapt<List<CharacterSkill>>();
            var skillToUse = skills.Where(x => characterSkills.Select(s => s.SkillVNum).Contains(x.SkillVNum));
            character.Skills.Clear();
            foreach (var characterSkill in characterSkills)
            {
                characterSkill.Skill = skillToUse.First(x => x.SkillVNum == characterSkill.SkillVNum);
                character.Skills.AddOrUpdate(characterSkill.SkillVNum, characterSkill,
                    (key, oldValue) => characterSkill);
            }

            // Push the refreshed list to the client. Matches OpenNos GenerateSki:
            // primary/secondary are the class starter vnums (200 + 20*Class, +1),
            // followed by every learned skill ordered by cast id so the bar draws
            // deterministically. Without this packet the client's hotbar stays empty
            // and the server's skill-cast gate is invisible.
            var classByte = (byte)character.Class;
            var ordered = character.Skills.Values
                .Where(s => s.Skill != null)
                .OrderBy(s => s.Skill!.CastId)
                .Select(s => s.SkillVNum)
                .ToList();

            await character.SendPacketAsync(new SkiPacket
            {
                PrimarySkillVnum = (short)(200 + 20 * classByte),
                SecondarySkillVnum = (short)(201 + 20 * classByte),
                SkillVnums = ordered,
            }).ConfigureAwait(false);
        }
    }
}
