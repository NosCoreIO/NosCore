//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Messaging.Events
{
    // Raised by MapChangeService when a player finishes changing into a new map instance.
    // Every handler with a Handle/HandleAsync(MapInstanceEnteredEvent) method runs and filters
    // it by map type or state itself.
    public sealed record MapInstanceEnteredEvent(ClientSession ClientSession, MapInstance MapInstance, MapInstance LeftMapInstance) : IGameEvent, IPlayerEvent
    {
        public bool PlayerHasLeft => !ClientSession.HasPlayerEntity;
    }
}
