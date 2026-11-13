//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.InstantCombatService
{
    public sealed record InstantCombatRoom(byte LevelMinimum, IReadOnlyList<ClientSession> Players);
}
