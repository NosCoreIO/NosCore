//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        var claims = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, serverConfiguration.Value.ToString()),
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
        return new HubConnectionBuilder()
            .WithAutomaticReconnect()
            .WithUrl($"{configuration.Value}/{name}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult((string?)handler.WriteToken(securityToken));
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new PolymorphicJsonConverter<IMessage>());
            })
            .Build();
    }
}
