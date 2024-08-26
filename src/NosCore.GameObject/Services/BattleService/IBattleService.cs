using NosCore.GameObject.ComponentEntities.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattleService
{
    public interface IBattleService
    {
        Task Hit(IAliveEntity origin, IAliveEntity target, HitArguments arguments);
    }
}
