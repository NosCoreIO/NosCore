using NosCore.GameObject.Ecs.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public interface ISkillService
    {
        Task LoadSkill(ICharacterEntity character);

        Task<bool> LearnClassSkillsAsync(ICharacterEntity character);
    }
}
