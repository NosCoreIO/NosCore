//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Moq;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;

namespace NosCore.Tests.Shared
{
    // A dispatcher that runs the given handler for every TEvent raised, before the raise returns.
    public sealed class InlineGameEvents<TEvent> where TEvent : IGameEvent
    {
        public InlineGameEvents(Func<TEvent, Task> handle)
        {
            var dispatcher = new Mock<IGameEventDispatcher>();
            dispatcher.Setup(d => d.RaiseAsync(It.IsAny<TEvent>())).Returns(handle);
            Object = dispatcher.Object;
        }

        public IGameEventDispatcher Object { get; }
    }
}
