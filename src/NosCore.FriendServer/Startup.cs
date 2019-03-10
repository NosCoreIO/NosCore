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

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastExpressionCompiler;
using JetBrains.Annotations;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NosCore.Configuration;
using NosCore.Core.Encryption;
using NosCore.Database;
using NosCore.Shared.I18N;
using Swashbuckle.AspNetCore.Swagger;
using System.ComponentModel.DataAnnotations;
using NosCore.FriendServer.Controllers;
using NosCore.Core.Controllers;
using NosCore.Database.DAL;

namespace NosCore.FriendServer
{
    public class Startup
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - FriendServer";
        private const string ConsoleText = "FRIEND SERVER - NosCoreIO";

        private FriendConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var friendConfiguration = new FriendConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("friend.json", false);
            builder.Build().Bind(friendConfiguration);
            Validator.ValidateObject(friendConfiguration, new ValidationContext(friendConfiguration),
                validateAllProperties: true);
            return friendConfiguration;
        }

        private ContainerBuilder InitializeContainer(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<FriendServer>().PropertiesAutowired();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();
            containerBuilder.Populate(services);
            return containerBuilder;
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            Console.Title = Title;
            Logger.PrintHeader(ConsoleText);
            var configuration = InitializeConfiguration();
            services.AddSingleton<IServerAddressesFeature>(new ServerAddressesFeature
            {
                PreferHostingUrls = true,
                Addresses = {configuration.WebApi.ToString()}
            });
            LogLanguage.Language = configuration.Language;
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info {Title = "NosCore Friend API", Version = "v1"}));
            var keyByteArray = Encoding.Default.GetBytes(configuration.MasterCommunication.Password.ToSha512());
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = signinKey,
                        ValidAudience = "Audience",
                        ValidIssuer = "Issuer",
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true
                    };
                });

            services.AddMvc(o =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    o.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddApplicationPart(typeof(TokenController).GetTypeInfo().Assembly)
                .AddApplicationPart(typeof(FriendController).GetTypeInfo().Assembly)
                .AddControllersAsServices();
            var containerBuilder = InitializeContainer(services);
            containerBuilder.RegisterInstance(configuration).As<FriendConfiguration>();
            containerBuilder.RegisterInstance(configuration.MasterCommunication).As<WebApiConfiguration>();
            var container = containerBuilder.Build();
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(configuration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
            Task.Run(() => container.Resolve<FriendServer>().Run());
            return new AutofacServiceProvider(container);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore Friend API"));
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}