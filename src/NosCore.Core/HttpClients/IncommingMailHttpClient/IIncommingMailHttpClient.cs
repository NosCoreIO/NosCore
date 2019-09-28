using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public interface IIncommingMailHttpClient
    {
        void NotifyIncommingMail(int channelId, MailData mailRequest);
        void OpenIncommingMail(int channelId, MailData mailData);
        void DeleteIncommingMail(int channelId, long id, short mailId, byte postType);
    }
}