using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Data.Enumerations;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;

namespace NosCore.Core
{
    public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IOptions<WebApiConfiguration> _webApiConfiguration;
        private readonly IHasher _hasher;

        public ConfigureJwtBearerOptions(IOptions<WebApiConfiguration> webApiConfiguration, IHasher hasher)
        {
            _webApiConfiguration = webApiConfiguration;
            _hasher = hasher;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var password = _hasher.Hash(_webApiConfiguration.Value.Password!, _webApiConfiguration.Value.Salt);
            if (name == JwtBearerDefaults.AuthenticationScheme)
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.Default.GetBytes(password)),
                    ValidAudience = "Audience",
                    ValidIssuer = "Issuer",
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };
            }
        }

        public void Configure(JwtBearerOptions options)
        {
            Configure(JwtBearerDefaults.AuthenticationScheme, options);
        }
    }
}
