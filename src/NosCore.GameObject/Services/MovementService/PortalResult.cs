//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.MovementService;

public enum PortalResult
{
    Used,
    // A portal in reach that can be taken now; only FindPortal answers it.
    Ready,
    TooSoon,
    NoPortal,
    EnemyCitadel,
    Locked,
}
