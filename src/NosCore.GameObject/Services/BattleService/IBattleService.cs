using Arch.Core;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattleService
{
    public interface IBattleService
    {
        Task Hit(PlayerContext player, Entity target, HitArguments arguments);
    }
}
