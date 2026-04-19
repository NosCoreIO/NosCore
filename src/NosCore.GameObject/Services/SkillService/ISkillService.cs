using NosCore.GameObject.Entities.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public interface ISkillService
    {
        Task LoadSkill(ICharacterEntity character);
    }
}
