using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;

namespace NosCore.Rpc
{
    public class HubClientBuilder
    {
        private readonly IHasher _hasher;

        public HubClientBuilder(IHasher hasher)
        {
            _hasher = hasher;
        }

        public HubConnection BuildClient(Hub hub, string password)
        {
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });

            var keyByteArray = Encoding.Default.GetBytes(_hasher.Hash(password));
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
                .WithUrl($"{conf}/{nameof(hub.GetType)}",
                    options => options.AccessTokenProvider = () => Task.FromResult(handler.WriteToken(securityToken)))
                .Build();
        }
    }
}
