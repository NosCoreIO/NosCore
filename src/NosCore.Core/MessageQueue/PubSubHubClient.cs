using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using Polly;
using Serilog;

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
                    options.AccessTokenProvider = () => Task.FromResult(handler.WriteToken(securityToken));
                })
                .Build();
        }

        public async Task BindAsync(Channel data, CancellationToken stoppingToken)
        {
            await _hubConnection.StartAsync(stoppingToken);
            try
            {
                await _hubConnection.InvokeAsync(nameof(BindAsync), data, stoppingToken, stoppingToken);
            }
            finally
            {
                await _hubConnection.StopAsync(stoppingToken);
            }
        }

        public Task<List<IMessage>> ReceiveMessagesAsync(int maxNumberOfMessages = 10, TimeSpan? visibilityTimeout = null)
        {
            return _hubConnection.InvokeAsync<List<IMessage>>(nameof(ReceiveMessagesAsync), maxNumberOfMessages, visibilityTimeout);
        }

        public Task DeleteMessageAsync(Guid messageId)
        {
            return _hubConnection.InvokeAsync(nameof(DeleteMessageAsync), messageId);
        }

        public Task SendMessageAsync(IMessage message)
        {
            return _hubConnection.InvokeAsync(nameof(ReceiveMessagesAsync), message);
        }

        public Task UpdateVisibilityTimeoutAsync(Guid messageId, TimeSpan visibilityTimeout)
        {
            return _hubConnection.InvokeAsync(nameof(UpdateVisibilityTimeoutAsync), messageId, visibilityTimeout);
        }
    }
}
