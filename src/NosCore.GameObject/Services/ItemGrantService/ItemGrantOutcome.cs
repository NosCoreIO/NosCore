//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.ItemGrantService;

public enum ItemGrantOutcome
{
    // Gold is granted with its own command, never as an item.
    Refused,
    UnknownItem,
    NotEnoughSpace,
}
