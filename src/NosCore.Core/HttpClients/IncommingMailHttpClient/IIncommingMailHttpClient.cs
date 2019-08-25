using System.Collections.Generic;
using NosCore.Configuration;
using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public interface IIncommingMailHttpClient
    {
        void NotifyIncommingMail(int channelId, MailData mailRequest);
    }
}
