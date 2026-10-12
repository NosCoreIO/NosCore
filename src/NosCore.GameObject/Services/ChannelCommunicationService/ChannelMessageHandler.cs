//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Shared.I18N;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService
{
    public static class ChannelMessageHandler
    {
        public static Task Handle(ChannelMessage channelMessage, IServiceProvider services)
        {
            var handler = services.GetServices<IChannelCommunicationMessageHandler<IMessage>>().FirstOrDefault(h =>
                h.GetType().BaseType?.GetGenericArguments().SingleOrDefault() == channelMessage.Message.GetType());
            if (handler != null)
            {
                return handler.Handle(channelMessage.Message);
            }

            services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ChannelMessageHandler))
                .LogWarning(services.GetRequiredService<ILogLanguageLocalizer<LogLanguageKey>>()[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
            return Task.CompletedTask;
        }
    }
}
