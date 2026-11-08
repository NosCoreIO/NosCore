//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.MovementService;

public enum WalkResult
{
    Accepted,
    // The client is already walking, so it has to be put back where the server has it.
    Refused,
    // The packet could not come from an honest client.
    Rejected,
}
