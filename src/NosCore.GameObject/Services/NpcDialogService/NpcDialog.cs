//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NodaTime;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.NpcDialogService
{
    // The window a player last opened through an NPC, a player shop or an item. A window opened
    // by an item has no owner and stays usable wherever the player stands on that map.
    public sealed record NpcDialog(
        NpcDialogWindow Window,
        VisualType? OwnerType,
        long OwnerId,
        Guid MapInstanceId,
        Instant OpenedAt);
}
