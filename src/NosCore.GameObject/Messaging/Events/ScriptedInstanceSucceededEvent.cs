//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;

namespace NosCore.GameObject.Messaging.Events
{
    // Published for each player still inside when a time-space or raid run is won. InstanceId is the
    // client's id for it, as the rbr window and the quest data use it.
    public sealed record ScriptedInstanceSucceededEvent(PlayerComponentBundle Character, ScriptedInstanceType Type, byte InstanceId)
        : IPlayerEvent
    {
        public PlayerComponentBundle Character { get => field.Live(); init; } = Character;

        public bool PlayerHasLeft => Character.HasLeft();
    }
}
