using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService
{
    public class CharacterSkill : CharacterSkillDto
    {
        public Instant? LastUse { get; set; }

        public required SkillDto Skill { get; set; }

        public bool CanBeUsed(Instant now)
        {
            return LastUse == null || LastUse.Value.Plus(Duration.FromMilliseconds(Skill.Cooldown * 100)) < now;
        }
    }
}
