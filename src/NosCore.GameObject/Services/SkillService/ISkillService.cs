using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public interface ISkillService
    {
        Task LoadSkill(PlayerContext player);
    }
}
