using NosCore.GameObject.ComponentEntities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public interface ISkillService
    {
        Task LoadSkill(ICharacterEntity character);
    }
}
