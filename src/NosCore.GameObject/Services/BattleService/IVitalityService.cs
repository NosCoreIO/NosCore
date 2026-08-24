//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

/// <summary>
/// A character's maximum HP and MP: the class and level base, plus what the worn equipment
/// and the active effects add.
/// </summary>
public interface IVitalityService
{
    /// <summary>
    /// Recomputes the maxima and writes them on the entity. True if they changed.
    /// </summary>
    bool Refresh(ICharacterEntity character);

    /// <summary>
    /// As <see cref="Refresh" />, and when something changed it sends the client the updated
    /// bar. Without the packet the server knows the new number and the player sees the old
    /// one: they would notice the difference only by taking a hit.
    /// </summary>
    Task RefreshAndNotifyAsync(ICharacterEntity character);
}
