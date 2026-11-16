//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Messaging
{
    // Handlers of one event run by ascending order, then by name.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GameEventOrderAttribute(int order) : Attribute
    {
        public int Order { get; } = order;
    }
}
