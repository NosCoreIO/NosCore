//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Networking.Encoding.Filter;
using NosCore.Networking.Resource;
using NosCore.Networking.SessionRef;
using NosCore.Shared.Configuration;
using NosCore.Shared.I18N;
using GameLogKey = NosCore.Data.Enumerations.I18N.LogLanguageKey;

namespace NosCore.GameObject.Hosting.Modules;

public sealed class NetworkingModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ClientSession>().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<SessionRefHolder>().AsImplementedInterfaces().SingleInstance();
        builder.RegisterType<SessionRegistry>().As<ISessionRegistry>().SingleInstance();
        builder.RegisterType<PacketBroadcaster>().As<IPacketBroadcaster>().SingleInstance();
        builder.RegisterType<PacketHandlerRegistry>().As<IPacketHandlerRegistry>().SingleInstance();
        builder.RegisterType<CharacterInitializationService>().As<ICharacterInitializationService>().SingleInstance();

        builder.Register(c =>
        {
            var lifetimeScope = c.Resolve<ILifetimeScope>();
            var logger = c.Resolve<ILogger<PipelineFactory>>();
            var logLanguage = c.Resolve<ILogLanguageLocalizer<GameLogKey>>();
            return new PipelineFactory(
                c.Resolve<IDecoder>(),
                c.Resolve<ISessionRefHolder>(),
                c.Resolve<IEnumerable<IRequestFilter>>(),
                c.Resolve<IPipelineConfiguration>(),
                () => lifetimeScope.Resolve<ClientSession>(),
                (package, client) => _ = DispatchPacketAsync((ClientSession)client, package, logger, logLanguage),
                client => _ = DispatchDisconnectAsync((ClientSession)client, logger, logLanguage),
                logger,
                c.Resolve<ILogLanguageLocalizer<LogLanguageKey>>()
            );
        }).SingleInstance();

        builder.Register(c => new NetworkManager(
            c.Resolve<IOptions<ServerConfiguration>>(),
            c.Resolve<PipelineFactory>(),
            c.Resolve<ILogger<NetworkManager>>(),
            c.Resolve<ILogLanguageLocalizer<LogLanguageKey>>()
        ));
    }

    private static async Task DispatchPacketAsync(ClientSession session, NosPackageInfo package, ILogger logger,
        ILogLanguageLocalizer<GameLogKey> logLanguage)
    {
        try
        {
            await session.HandlePacketAsync(package);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, logLanguage[NosCore.Data.Enumerations.I18N.LogLanguageKey.PACKET_PROCESSING_FAILED], session.SessionId);
        }
    }

    private static async Task DispatchDisconnectAsync(ClientSession session, ILogger logger,
        ILogLanguageLocalizer<GameLogKey> logLanguage)
    {
        try
        {
            await session.OnDisconnectedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, logLanguage[NosCore.Data.Enumerations.I18N.LogLanguageKey.SESSION_DISCONNECT_FAILED], session.SessionId);
        }
    }
}
