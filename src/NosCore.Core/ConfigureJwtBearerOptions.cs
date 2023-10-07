//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using System;
using System.Text;

namespace NosCore.Core
{
    public class ConfigureJwtBearerOptions(IOptions<WebApiConfiguration> webApiConfiguration, IHasher hasher)
        : IConfigureNamedOptions<JwtBearerOptions>
    {
        public void Configure(string? name, JwtBearerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var password = hasher.Hash(webApiConfiguration.Value.Password!, webApiConfiguration.Value.Salt);
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
