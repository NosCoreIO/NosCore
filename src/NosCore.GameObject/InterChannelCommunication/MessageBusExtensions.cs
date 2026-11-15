//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Observability;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication;

public static class MessageBusExtensions
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static async Task<T> RequestAsync<T>(this IMessageBus bus, IMasterRequest request) =>
        (await bus.InvokeMasterAsync<Reply<T>>(request)).Value;

    public static Task<TReply> InvokeMasterAsync<TReply>(this IMessageBus bus, IMasterRequest request) =>
        MeasureAsync(request, () => bus.InvokeAsync<TReply>(request, timeout: Timeout));

    public static Task SendToMasterAsync(this IMessageBus bus, IMasterRequest request) =>
        MeasureAsync(request, async () =>
        {
            await bus.InvokeAsync(request, timeout: Timeout);
            return true;
        });

    private static async Task<T> MeasureAsync<T>(IMasterRequest request, Func<Task<T>> invoke)
    {
        var type = new KeyValuePair<string, object?>("message.type", request.GetType().Name);
        var started = Stopwatch.GetTimestamp();
        try
        {
            return await invoke();
        }
        catch (TimeoutException)
        {
            NosCoreMetrics.MasterRequestTimeouts.Add(1, type);
            throw;
        }
        finally
        {
            NosCoreMetrics.MasterRequestDuration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, type);
        }
    }

    public static Uri QueueUri(this MessagingConfiguration configuration, string queueName) =>
        configuration.Transport == MessagingTransport.RabbitMq
            ? new Uri($"rabbitmq://queue/{queueName}")
            : new Uri($"postgresql://{queueName}");
}
