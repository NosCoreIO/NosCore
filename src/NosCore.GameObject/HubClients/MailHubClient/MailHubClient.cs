using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Json.Patch;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.HubInterfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.MailHubClient
{
    public class MailHubClient : IMailHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public MailHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{worldConfiguration.Value}/{nameof(IChannelHub).TrimStart('I')}")
                .Build();
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            return _hubConnection.StartAsync(stoppingToken);
        }

        public Task SendGiftAsync(ICharacterEntity characterEntity, long receiverId, IItemInstanceDto itemInstance, bool isNosmall)
        {
            throw new System.NotImplementedException();
        }

        public Task SendGiftAsync(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare,
            byte upgrade, bool isNosmall)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<MailData>> GetGiftsAsync(long characterId)
        {
            throw new System.NotImplementedException();
        }

        public Task<MailData?> GetGiftAsync(long id, long characterId, bool isCopy)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteGiftAsync(long giftId, long visualId, bool isCopy)
        {
            throw new System.NotImplementedException();
        }

        public Task ViewGiftAsync(long giftId, JsonPatch mailData)
        {
            throw new System.NotImplementedException();
        }

        public Task SendMessageAsync(ICharacterEntity character, long characterId, string title, string text)
        {
            throw new System.NotImplementedException();
        }

        public Task NotifyIncommingMailAsync(int channelId, MailData mailRequest)
        {
            throw new System.NotImplementedException();
        }

        public Task OpenIncommingMailAsync(int channelId, MailData mailData)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteIncommingMailAsync(int channelId, long id, short mailId, byte postType)
        {
            throw new System.NotImplementedException();
        }
    }
}
