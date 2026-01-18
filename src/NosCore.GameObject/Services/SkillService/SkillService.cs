using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Services.SkillService
{
    public class SkillService(IDao<CharacterSkillDto, Guid> characterSkillDao, List<SkillDto> skills) : ISkillService
    {
        public Task LoadSkill(PlayerContext player)
        {
            var characterSkills = characterSkillDao.Where(x => x.CharacterId == player.VisualId).Adapt<List<CharacterSkill>>();
            var skillToUse = skills.Where(x => characterSkills.Select(s => s.SkillVNum).Contains(x.SkillVNum));
            player.Skills.Clear();
            foreach (var characterSkill in characterSkills)
            {
                characterSkill.Skill = skillToUse.First(x => x.SkillVNum == characterSkill.SkillVNum);
                player.Skills.AddOrUpdate(characterSkill.SkillVNum, characterSkill,
                    (key, oldValue) => characterSkill);
            }

            return Task.CompletedTask;
        }
    }
}
