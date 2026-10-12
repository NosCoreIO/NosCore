//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using JasperFx.Core;
using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.Shared.Configuration;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using IMessage = NosCore.GameObject.InterChannelCommunication.Messages.IMessage;

namespace NosCore.GameObject.Messaging
{
    public static class WolverineHostExtensions
    {
        // Pinning ApplicationAssembly scopes Wolverine's default scan so it doesn't
        // sweep every loaded native runtime DLL looking for IWolverineExtension impls
        // — that's what produced the "To disable automatic Wolverine extension finding"
        // startup notice. Handler convention stays enabled unless the caller turns it
        // off, so new *Handler classes are picked up without an explicit registration.
        public static IHostBuilder UseNosCoreWolverine(this IHostBuilder builder, IConfiguration configuration,
            InterChannelNode node, Action<WolverineOptions>? configure = null)
        {
            var messaging = configuration.GetSection("Messaging").Get<MessagingConfiguration>() ?? new MessagingConfiguration();
            var database = configuration.GetSection("Database").Get<SqlConnectionConfiguration>()
                ?? throw new InvalidOperationException("Database configuration is required for inter-channel messaging");

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(messaging);
                services.AddSingleton(node);
            });

            return builder.UseWolverine(opts =>
            {
                opts.ServiceName = node.ServiceName;
                opts.ApplicationAssembly = typeof(WolverineHostExtensions).Assembly;

                // ExtensionDiscovery.ManualOnly below stops WolverineFx.RuntimeCompilation from
                // self-registering, so it has to be invoked here.
                opts.UseRuntimeCompilation();

                // Wolverine's code generation only sees the IServiceCollection, not Autofac, so
                // handlers backed by Autofac registrations take IServiceProvider and resolve there.
                opts.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;
                opts.UseSystemTextJsonForSerialization(json => json.Converters.Add(new PolymorphicJsonConverter<IMessage>()));
                ConfigureTransport(opts, messaging, database, node);
                configure?.Invoke(opts);
            }, ExtensionDiscovery.ManualOnly);
        }

        private static void ConfigureTransport(WolverineOptions opts, MessagingConfiguration messaging,
            SqlConnectionConfiguration database, InterChannelNode node)
        {
            if (messaging.Transport == MessagingTransport.RabbitMq)
            {
                opts.UseRabbitMq(new Uri(messaging.RabbitMqUri
                    ?? throw new InvalidOperationException("Messaging.RabbitMqUri is required when Messaging.Transport is RabbitMq")))
                    .AutoProvision();
                opts.ListenToRabbitQueue(node.QueueName).BufferedInMemory();
                opts.Publish(rule =>
                {
                    rule.MessagesImplementing<IMasterRequest>();
                    rule.ToRabbitQueue(InterChannelNode.MasterQueue).BufferedInMemory();
                });
                return;
            }

            // Each node keeps its envelope storage in its own schema so a node never
            // recovers envelopes addressed to another one; only the queue tables are shared.
            opts.UsePostgresqlPersistenceAndTransport(database.ConnectionString, $"wolverine_{node.QueueName}",
                    transportSchema: "wolverine_queues")
                .AutoProvision();
            opts.Durability.Mode = DurabilityMode.Solo;
            opts.ListenToPostgresqlQueue(node.QueueName)
                .PollingInterval(messaging.PollingIntervalMs.Milliseconds())
                .BufferedInMemory();
            opts.Publish(rule =>
            {
                rule.MessagesImplementing<IMasterRequest>();
                rule.ToPostgresqlQueue(InterChannelNode.MasterQueue).BufferedInMemory();
            });
        }
    }
}
