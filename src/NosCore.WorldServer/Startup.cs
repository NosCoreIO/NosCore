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
using NosCore.Core.Encryption;
using NosCore.Database;
using NosCore.GameObject.Event;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.WorldServer.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastExpressionCompiler;
using NosCore.Core;
using NosCore.Core.Controllers;
using NosCore.Core.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.DependancyInjection;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.Data.DataAttributes;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.DAL;
using NosCore.GameObject.Providers.MapInstanceProvider;
using Item = NosCore.GameObject.Providers.ItemProvider.Item.Item;
using AutofacSerilogIntegration;
using ChickenAPI.Packets.ClientPackets.Npcs;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.UI;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Configuration;
using NosCore.Data.CommandPackets;
using NosCore.Data.GraphQL;
using NosCore.PacketHandlers.Login;
using NosCore.WorldServer.GraphQl;
using NosCore.Data.WebApi;
using Character = NosCore.GameObject.Character;

namespace NosCore.WorldServer
{
    public class Startup
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - WorldServer";
        private const string ConsoleText = "WORLD SERVER - NosCoreIO";

        private static WorldConfiguration _worldConfiguration;

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            _worldConfiguration = new WorldConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("world.json", false);
            builder.Build().Bind(_worldConfiguration);
            Validator.ValidateObject(_worldConfiguration, new ValidationContext(_worldConfiguration),
                validateAllProperties: true);

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(_worldConfiguration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

            LogLanguage.Language = _worldConfiguration.Language;
        }

        public static void RegisterMapper<TGameObject, TDto>(IContainer container) => TypeAdapterConfig<TDto, TGameObject>.NewConfig().ConstructUsing(src => container.Resolve<TGameObject>());

        public static void RegisterDatabaseObject<TDto, TDb>(ContainerBuilder containerBuilder, bool isStatic) where TDb : class
        {
            containerBuilder.RegisterType<GenericDao<TDb, TDto>>().As<IGenericDao<TDto>>().SingleInstance();
            if (isStatic)
            {
                StaticDtoAttribute staticDtoAttribute = typeof(TDto).GetCustomAttribute<StaticDtoAttribute>();
                containerBuilder.Register(c =>
                    {
                        var items = c.Resolve<IGenericDao<TDto>>().LoadAll().ToList();
                        if (items.Count != 0 || (staticDtoAttribute == null || staticDtoAttribute.EmptyMessage == LogLanguageKey.UNKNOWN))
                        {
                            if (staticDtoAttribute != null && staticDtoAttribute.LoadedMessage != LogLanguageKey.UNKNOWN)
                            {
                                c.Resolve<Serilog.ILogger>().Information(LogLanguage.Instance.GetMessageFromKey(staticDtoAttribute.LoadedMessage),
                                    items.Count);
                            }
                        }
                        else
                        {
                            c.Resolve<Serilog.ILogger>().Error(LogLanguage.Instance.GetMessageFromKey(staticDtoAttribute.EmptyMessage));
                        }

                        return items;
                    })
                    .As<List<TDto>>()
                    .SingleInstance()
                    .AutoActivate();
            }
        }

        private static void RegisterGo(IContainer container)
        {
            var registerMapper = typeof(Startup).GetMethod(nameof(RegisterMapper));
            var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
            var assemblyGo = typeof(Character).Assembly.GetTypes();

            assemblyDto.Where(p => typeof(IDto).IsAssignableFrom(p) && p.IsClass)
                .ToList()
                .ForEach(t =>
                {
                    assemblyGo.Where(p => (t.IsAssignableFrom(p))).ToList().ForEach(tgo =>
                    {
                        registerMapper.MakeGenericMethod(tgo, t).Invoke(null, new[] { container });
                    });
                });
        }

        private static void RegisterDto(ContainerBuilder containerBuilder)
        {
            var registerDatabaseObject = typeof(Startup).GetMethod(nameof(RegisterDatabaseObject));
            var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
            var assemblyDb = typeof(Database.Entities.Account).Assembly.GetTypes();

            assemblyDto.Where(p => typeof(IDto).IsAssignableFrom(p) && !p.Name.Contains("InstanceDto") && p.IsClass)
                .ToList()
                .ForEach(t =>
                {
                    var type = assemblyDb.First(tgo =>
                        string.Compare(t.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
                    registerDatabaseObject.MakeGenericMethod(t, type).Invoke(null, new[] { containerBuilder, (object)typeof(IStaticDto).IsAssignableFrom(t) });
                });

            containerBuilder.RegisterType<ItemInstanceDao>().As<IGenericDao<IItemInstanceDto>>().SingleInstance();
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<Adapter>().AsImplementedInterfaces().PropertiesAutowired();
            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList();
            listofpacket.AddRange(typeof(HelpPacket).Assembly.GetTypes()
                .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList());
            containerBuilder.Register(c => new Deserializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.Register(c => new Serializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            //NosCore.Configuration
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterInstance(_worldConfiguration).As<WorldConfiguration>().As<ServerConfiguration>();
            containerBuilder.RegisterInstance(_worldConfiguration.MasterCommunication).As<WebApiConfiguration>();

            //NosCore.Controllers
            foreach (var type in typeof(NoS0575PacketHandler).Assembly.GetTypes())
            {
                if (typeof(IPacketHandler).IsAssignableFrom(type) && typeof(IWorldPacketHandler).IsAssignableFrom(type))
                {
                    containerBuilder.RegisterType(type)
                        .AsImplementedInterfaces()
                        .PropertiesAutowired();
                }
            }

            //NosCore.Core
            containerBuilder.RegisterType<WorldDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<WorldEncoder>().As<MessageToMessageEncoder<IEnumerable<IPacket>>>();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();

            //NosCore.WorldServer
            containerBuilder.RegisterType<WorldServer>().PropertiesAutowired();

            //NosCore.GameObject
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
            RegisterDto(containerBuilder);

            containerBuilder.RegisterAssemblyTypes(typeof(Character).Assembly)
                .Where(t => typeof(IDto).IsAssignableFrom(t))
                .AsSelf()
                .PropertiesAutowired();


            containerBuilder.RegisterAssemblyTypes(typeof(IGlobalEvent).Assembly)
                .Where(t => typeof(IGlobalEvent).IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>).Assembly)
                .Where(t => typeof(IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>).IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IEventHandler<MapItem, Tuple<MapItem, GetPacket>>).Assembly)
                .Where(t => typeof(IEventHandler<MapItem, Tuple<MapItem, GetPacket>>).IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder
                .RegisterAssemblyTypes(
                    typeof(IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>).Assembly)
                .Where(t => typeof(IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>)
                    .IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IEventHandler<GuriPacket, GuriPacket>).Assembly)
                .Where(t => typeof(IEventHandler<GuriPacket, GuriPacket>).IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();
        }


        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            Console.Title = Title;
            Logger.PrintHeader(ConsoleText);
            //PacketFactory.Initialize<NoS0575Packet>();
            InitializeConfiguration();

            services.AddSingleton<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));
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
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.Default.GetBytes(_worldConfiguration.MasterCommunication.Password.ToSha512())),
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

            services.AddSingleton<Query>();
            services.AddGraphQL(sp => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(sp);
                    c.RegisterAuthorizeDirectiveType();
                    c.RegisterExtendedScalarTypes();

                    c.RegisterQueryType<QueryType>();

                    c.RegisterType<ConnectedAccountType>();
                    c.RegisterType<CharacterType>();
                }),
                new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.Always
                });

            var containerBuilder = new ContainerBuilder();
            InitializeContainer(containerBuilder);
            containerBuilder.Populate(services);
            var container = containerBuilder.Build();

            RegisterGo(container);
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>().AfterMapping(dest => Task.Run(() => dest.Initialize()));
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();

            container.Resolve<IMapInstanceProvider>().Initialize();
            Task.Run(() => container.Resolve<WorldServer>().Run());
            return new AutofacServiceProvider(container);
        }


        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets()
                .UseGraphQL(new QueryMiddlewareOptions
                {
                    Path = "/graphql",
                    OnCreateRequest = (ctx, builder, ct) => Task.CompletedTask
                });

            app.UseMvc();
        }
    }
}