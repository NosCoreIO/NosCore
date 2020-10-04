using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Data.Enumerations;

namespace NosCore.Core
{
    public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IOptions<WebApiConfiguration> _webApiConfiguration;

        public ConfigureJwtBearerOptions(IOptions<WebApiConfiguration> webApiConfiguration)
        {
            _webApiConfiguration = webApiConfiguration;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var password = _webApiConfiguration.Value.HashingType switch
            {
                HashingType.BCrypt => _webApiConfiguration.Value.Password!.ToBcrypt(_webApiConfiguration.Value
                    .Salt ?? ""),
                HashingType.Pbkdf2 => _webApiConfiguration.Value.Password!.ToPbkdf2Hash(_webApiConfiguration.Value
                    .Salt ?? ""),
                HashingType.Sha512 => _webApiConfiguration.Value.Password!.ToSha512(),
                _ => _webApiConfiguration.Value.Password!.ToSha512()
            };
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
