//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNetty.Buffers;
using DotNetty.Codecs;
using FastExpressionCompiler;
using JetBrains.Annotations;
using log4net;
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
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.DAL;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.Inventory;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.I18N;
using NosCore.WorldServer.Controllers;
using Swashbuckle.AspNetCore.Swagger;

namespace NosCore.WorldServer
{
    public class Startup
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - WorldServer";
        const string consoleText = "WORLD SERVER - NosCoreIO";

        private static WorldConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var worldConfiguration = new WorldConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("world.json", false);
            builder.Build().Bind(worldConfiguration);
            return worldConfiguration;
        }

        private static void InitializeContainer(ref ContainerBuilder containerBuilder, IServiceCollection services)
        {
            containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly).As<IPacketController>();
            containerBuilder.RegisterType<WorldDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<WorldEncoder>().As<MessageToMessageEncoder<string>>();
            containerBuilder.RegisterType<WorldServer>().PropertiesAutowired();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();

            containerBuilder.RegisterAssemblyTypes(typeof(InventoryService).Assembly)
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces()
                .PropertiesAutowired();

            containerBuilder.Register(_ =>
            {
                var items = DAOFactory.ItemDAO.LoadAll().Adapt<List<Item>>().ToList();
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ITEMS_LOADED),
                    items.Count));
                return items;
            }).As<List<Item>>().SingleInstance();
            containerBuilder.Register(_ =>
            {
                List<NpcMonsterDTO> monsters = DAOFactory.NpcMonsterDAO.LoadAll().ToList();
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NPCMONSTERS_LOADED),
                    monsters.Count));
                return monsters;
            }).As<List<NpcMonsterDTO>>().SingleInstance();
            containerBuilder.Register(_ =>
            {
                List<Map> maps = DAOFactory.MapDAO.LoadAll().Adapt<List<Map>>();
                if (maps.Count != 0)
                {
                    Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPS_LOADED),
                        maps.Count));
                }
                else
                {
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NO_MAP));
                }

                return maps;
            }).As<List<Map>>().SingleInstance();

            containerBuilder.RegisterType<MapInstanceAccessService>().SingleInstance();

            containerBuilder.Populate(services);
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            Console.Title = Title;
            Logger.InitializeLogger(LogManager.GetLogger(typeof(WorldServer)));
            Logger.PrintHeader(consoleText);
            PacketFactory.Initialize<NoS0575Packet>();
            var configuration = InitializeConfiguration();

            services.AddSingleton<IServerAddressesFeature>(new ServerAddressesFeature
            {
                PreferHostingUrls = true,
                Addresses = {configuration.WebApi.ToString()}
            });
            LogLanguage.Language = configuration.Language;
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info {Title = "NosCore World API", Version = "v1"}));
            var keyByteArray =
                Encoding.Default.GetBytes(EncryptionHelper.Sha512(configuration.MasterCommunication.Password));
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
            }).AddApplicationPart(typeof(TokenController).GetTypeInfo().Assembly).AddControllersAsServices();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(configuration).As<WorldConfiguration>().As<GameServerConfiguration>();
            containerBuilder.RegisterInstance(configuration.MasterCommunication).As<MasterCommunicationConfiguration>();
            InitializeContainer(ref containerBuilder, services);
            var container = containerBuilder.Build();
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(configuration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
            Task.Run(() => container.Resolve<WorldServer>().Run());
            return new AutofacServiceProvider(container);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore World API"));
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}