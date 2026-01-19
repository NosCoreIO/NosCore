using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using System;

namespace NosCore.GameObject.Services.BattleService
{
    public class CharacterSkill : CharacterSkillDto
    {
        public CharacterSkill()
        {
            LastUse = DateTime.Now.AddHours(-1);
        }

        public DateTime LastUse
        {
            get; set;
        }

        public SkillDto? Skill { get; set; }

        public bool CanBeUsed()
        {
            return Skill != null && LastUse.AddMilliseconds(Skill.Cooldown * 100) < DateTime.Now;
        }
    }
}
