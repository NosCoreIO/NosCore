//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Messaging.Events
{
    // Sample domain event published by the combat path. Wolverine discovers handlers via convention
    // (any class with a Handle/HandleAsync method whose first parameter is this type).
    public sealed record MonsterKilledEvent(
        long MonsterMapId,
        short MonsterVNum,
        long? KillerCharacterId,
        Guid MapInstanceId);
}
