using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public interface IIncommingMailHttpClient
    {
        void NotifyIncommingMail(int channelId, MailData mailRequest);
    }
}
