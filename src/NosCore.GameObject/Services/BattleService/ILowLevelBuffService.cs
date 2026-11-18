//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.BattleService;

public interface ILowLevelBuffService
{
    /// <summary>Gives a living character up to the last eligible level the buff it lacks, and takes it off one past that level.</summary>
    Task RefreshAsync(PlayerComponentBundle character);

    /// <summary>Gives the buff again when the card that ran out is that buff.</summary>
    Task RenewAsync(PlayerComponentBundle character, short cardId);
}
