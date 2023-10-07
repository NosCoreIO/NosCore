using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Data.WebApi;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Core.MessageQueue
{
    public class PubSubHubClient : IPubSubHub
    {
        private readonly HubConnection _hubConnection;

        public PubSubHubClient(IOptions<WebApiConfiguration> configuration, IHasher hasher)
        {
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });
            var password = hasher.Hash(configuration.Value.Password!, configuration.Value.Salt);

            var keyByteArray = Encoding.Default.GetBytes(password);
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
            });
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{configuration.Value}/{nameof(PubSubHub)}", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult((string?)handler.WriteToken(securityToken));
                })
                .Build();
        }

        public async Task Bind(Channel data)
        {
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync(nameof(Bind), data);
        }

        public Task<List<ChannelInfo>> GetCommunicationChannels()
        {
            return _hubConnection.InvokeAsync<List<ChannelInfo>>(nameof(GetCommunicationChannels));
        }

        public Task<List<IMessage>> ReceiveMessagesAsync(int maxNumberOfMessages = 10, TimeSpan? visibilityTimeout = null)
        {
            return _hubConnection.InvokeAsync<List<IMessage>>(nameof(ReceiveMessagesAsync), maxNumberOfMessages);
        }

        public Task DeleteMessageAsync(Guid messageId)
        {
            return _hubConnection.InvokeAsync(nameof(DeleteMessageAsync), messageId);
        }

        public Task<bool> SendMessageAsync(IMessage message)
        {
            return _hubConnection.InvokeAsync<bool>(nameof(ReceiveMessagesAsync), message);
        }

        public Task<List<Subscriber>> GetSubscribersAsync()
        {
            return _hubConnection.InvokeAsync<List<Subscriber>>(nameof(GetSubscribersAsync));
        }

        public Task SubscribeAsync(Subscriber subscriber)
        {
            return _hubConnection.InvokeAsync(nameof(SubscribeAsync), subscriber);
        }

        public Task UnsubscribeAsync(long id)
        {
            return _hubConnection.InvokeAsync(nameof(UnsubscribeAsync), id);
        }
    }
}