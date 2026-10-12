//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication;

public class HubConnectionFactory(IOptions<ServerConfiguration> serverConfiguration, IOptions<WebApiConfiguration> configuration, IHasher hasher)
{
    public HubConnection Create(string name)
    {
        var password = hasher.Hash(configuration.Value.Password!, configuration.Value.Salt);
        var signinKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(password));
        var handler = new JwtSecurityTokenHandler();

        string CreateToken() => handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, serverConfiguration.Value.ToString()),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            }),
            Issuer = "Issuer",
            Audience = JwtAudience.Service,
            SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
        }));

        return new HubConnectionBuilder()
            .WithAutomaticReconnect()
            .WithUrl($"{configuration.Value}/{name}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(CreateToken());
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new PolymorphicJsonConverter<IMessage>());
            })
            .Build();
    }
}
