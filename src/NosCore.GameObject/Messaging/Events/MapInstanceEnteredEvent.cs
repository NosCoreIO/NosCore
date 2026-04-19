//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Messaging.Events
{
    // Published by MapChangeService when a player finishes changing into a new map instance.
    // Wolverine fans out to every handler with a Handle/HandleAsync(MapInstanceEnteredEvent)
    // method; each handler filters by map type / state internally.
    public sealed record MapInstanceEnteredEvent(ClientSession ClientSession, MapInstance MapInstance);
}
