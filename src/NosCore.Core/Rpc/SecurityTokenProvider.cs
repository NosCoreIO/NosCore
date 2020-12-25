using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using NosCore.Shared.Authentication;
using NosCore.Shared.Enumerations;

namespace NosCore.Core.Rpc
{
    public class SecurityTokenProvider
    {
        private readonly IHasher _hasher;

        public SecurityTokenProvider(IHasher hasher)
        {
            _hasher = hasher;
        }

        public string GenerateSecurityToken(string password, string? salt)
        {
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });

            var keyByteArray = Encoding.Default.GetBytes(_hasher.Hash(password, salt));
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
            });

            return handler.WriteToken(securityToken);
        }
    }
}
