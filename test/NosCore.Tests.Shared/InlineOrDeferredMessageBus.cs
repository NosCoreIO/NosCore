//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Wolverine;

namespace NosCore.Tests.Shared
{
    // InvokeAsync runs the handlers inline like Wolverine does; PublishAsync only queues the
    // message, the way a local queue hands it to another thread later.
    public sealed class InlineOrDeferredMessageBus<TMessage>
    {
        private readonly ConcurrentQueue<TMessage> _published = new();
        private readonly Func<TMessage, Task> _dispatch;

        public InlineOrDeferredMessageBus(Func<TMessage, Task> dispatch)
        {
            _dispatch = dispatch;
            var bus = new Mock<IMessageBus>();
            bus.Setup(b => b.PublishAsync(It.IsAny<TMessage>(), It.IsAny<DeliveryOptions?>()))
                .Callback<TMessage, DeliveryOptions?>((message, _) => _published.Enqueue(message))
                .Returns(ValueTask.CompletedTask);
            bus.Setup(b => b.InvokeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
                .Returns<object, CancellationToken, TimeSpan?>((message, _, _) => _dispatch((TMessage)message));
            Object = bus.Object;
        }

        public IMessageBus Object { get; }

        public async Task DeliverPublishedAsync()
        {
            while (_published.TryDequeue(out var message))
            {
                await _dispatch(message);
            }
        }
    }
}
