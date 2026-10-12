//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication;

public static class MessageBusExtensions
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static async Task<T> RequestAsync<T>(this IMessageBus bus, IMasterRequest request) =>
        (await bus.InvokeAsync<Reply<T>>(request, timeout: Timeout)).Value;

    public static Task SendToMasterAsync(this IMessageBus bus, IMasterRequest request) =>
        bus.InvokeAsync(request, timeout: Timeout);

    public static Uri QueueUri(this MessagingConfiguration configuration, string queueName) =>
        configuration.Transport == MessagingTransport.RabbitMq
            ? new Uri($"rabbitmq://queue/{queueName}")
            : new Uri($"postgresql://{queueName}");
}
