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
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject.Event;
using NosCore.GameObject.Map;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.I18N;
using NosCore.WorldServer.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using System.ComponentModel.DataAnnotations;
using NosCore.Core.Controllers;
using NosCore.Data.AliveEntities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.DependancyInjection;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapItemProvider;

namespace NosCore.WorldServer
{
    public class Startup
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - WorldServer";
        private const string ConsoleText = "WORLD SERVER - NosCoreIO";
        private static readonly Serilog.ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private static WorldConfiguration _worldConfiguration;

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            _worldConfiguration = new WorldConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("world.json", false);
            builder.Build().Bind(_worldConfiguration);
            Validator.ValidateObject(_worldConfiguration, new ValidationContext(_worldConfiguration), validateAllProperties: true);

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(_worldConfiguration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

            LogLanguage.Language = _worldConfiguration.Language;
        }

        private static void InitializeContainer(ref ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<Adapter>().AsImplementedInterfaces().PropertiesAutowired();

            //NosCore.Configuration
            containerBuilder.RegisterInstance(_worldConfiguration).As<WorldConfiguration>().As<ServerConfiguration>();
            containerBuilder.RegisterInstance(_worldConfiguration.MasterCommunication).As<WebApiConfiguration>();

            //NosCore.Controllers
            containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly).As<IPacketController>();

            //NosCore.Core
            containerBuilder.RegisterType<WorldDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<WorldEncoder>().As<MessageToMessageEncoder<string>>();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();

            //NosCore.WorldServer
            containerBuilder.RegisterType<WorldServer>().PropertiesAutowired();

            //NosCore.GameObject
            containerBuilder.RegisterType<Character>().PropertiesAutowired();
            containerBuilder.RegisterType<Mapper>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();
            containerBuilder.RegisterAssemblyTypes(typeof(IInventoryService).Assembly)
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces()
                .PropertiesAutowired();
            containerBuilder.RegisterAssemblyTypes(typeof(IExchangeProvider).Assembly)
                .Where(t => t.Name.EndsWith("Provider"))
                .AsImplementedInterfaces()
                .SingleInstance()
                .PropertiesAutowired();

            containerBuilder.Register(_ =>
            {
                var items = DaoFactory.ItemDao.LoadAll().Adapt<List<Item>>().ToList();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ITEMS_LOADED),
                    items.Count);
                return items;
            }).As<List<Item>>().SingleInstance();

            
            containerBuilder.Register(_ =>
            {
                List<NpcMonsterDto> monsters = DaoFactory.NpcMonsterDao.LoadAll().ToList();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NPCMONSTERS_LOADED), monsters.Count);
                return monsters;
            }).As<List<NpcMonsterDto>>().SingleInstance();

            containerBuilder.Register(_ =>
            {
                var shopItems = DaoFactory.ShopItemDao.LoadAll().ToList();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SHOPITEMS_LOADED), shopItems.Count);
                return shopItems;
            }).As<List<ShopItemDto>>().SingleInstance();

            containerBuilder.Register(_ =>
            {
                var shops = DaoFactory.ShopDao.LoadAll().ToList();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SHOPS_LOADED), shops.Count);
                return shops;
            }).As<List<ShopDto>>().SingleInstance();

            containerBuilder.Register(_ =>
            {
                List<Map> maps = DaoFactory.MapDao.LoadAll().Adapt<List<Map>>();
                if (maps.Count != 0)
                {
                    _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPS_LOADED),
                        maps.Count);
                }
                else
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NO_MAP));
                }

                return maps;
            }).As<List<Map>>().SingleInstance();

            containerBuilder.Register(_ =>
            {
                List<MapMonsterDto> monsters = DaoFactory.MapMonsterDao.LoadAll().ToList();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPMONSTERS_LOADED), monsters.Count);
                return monsters;
            }).As<List<MapMonsterDto>>().SingleInstance();

            containerBuilder.Register(_ =>
            {
                List<MapNpcDto> npcs = DaoFactory.MapNpcDao.LoadAll().ToList();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPNPCS_LOADED), npcs.Count);
                return npcs;
            }).As<List<MapNpcDto>>().SingleInstance();

            containerBuilder.RegisterAssemblyTypes(typeof(IGlobalEvent).Assembly)
                .Where(t => typeof(IGlobalEvent).IsAssignableFrom(t))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IHandler<Item, Tuple<IItemInstance, UseItemPacket>>).Assembly)
                .Where(t => typeof(IHandler<Item, Tuple<IItemInstance, UseItemPacket>>).IsAssignableFrom(t))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IHandler<MapItem, Tuple<MapItem, GetPacket>>).Assembly)
                .Where(t => typeof(IHandler<MapItem, Tuple<MapItem, GetPacket>>).IsAssignableFrom(t))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>).Assembly)
                .Where(t => typeof(IHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>).IsAssignableFrom(t))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IHandler<GuriPacket, GuriPacket>).Assembly)
                .Where(t => typeof(IHandler<GuriPacket, GuriPacket>).IsAssignableFrom(t))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();     
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            Console.Title = Title;
            Logger.PrintHeader(ConsoleText);
            PacketFactory.Initialize<NoS0575Packet>();
            InitializeConfiguration();

            services.AddSingleton<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info { Title = "NosCore World API", Version = "v1" }));
            services.AddSingleton<IServerAddressesFeature>(new ServerAddressesFeature
            {
                PreferHostingUrls = true,
                Addresses = { _worldConfiguration.WebApi.ToString() }
            });
            services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(_worldConfiguration.MasterCommunication.Password.ToSha512())),
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
                .AddApplicationPart(typeof(StatController).GetTypeInfo().Assembly)
                .AddApplicationPart(typeof(TokenController).GetTypeInfo().Assembly)
                .AddControllersAsServices();


            var containerBuilder = new ContainerBuilder();
            InitializeContainer(ref containerBuilder);
            containerBuilder.Populate(services);
            var container = containerBuilder.Build();

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