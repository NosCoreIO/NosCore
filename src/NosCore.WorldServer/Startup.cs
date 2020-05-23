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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Interfaces;
using DotNetty.Buffers;
using DotNetty.Codecs;
using FastExpressionCompiler;
using FastMember;
using JetBrains.Annotations;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.Threading;
using NosCore.Algorithm.ExperienceService;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.I18N;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Database.Entities.Base;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Event;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.Login;
using NosCore.WorldServer.Controllers;
using Character = NosCore.GameObject.Character;
using Deserializer = NosCore.Packets.Deserializer;
using ILogger = Serilog.ILogger;
using InventoryItemInstance = NosCore.GameObject.Providers.InventoryService.InventoryItemInstance;
using Item = NosCore.GameObject.Providers.ItemProvider.Item.Item;
using Serializer = NosCore.Packets.Serializer;
using NosCore.Dao;
using NosCore.Data.Dto;
using NosCore.GameObject.Configuration;
using NosCore.Packets.Enumerations;
using NosCore.PathFinder;
using NosCore.Shared.Configuration;
using ItemInstance = NosCore.Database.Entities.ItemInstance;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer
{
    public class Startup
    {
        private const string Title = "NosCore - WorldServer";

        private static readonly WorldConfiguration _worldConfiguration = new WorldConfiguration();
        private static DataAccessHelper _dataAccess = null!;

        public Startup(IConfiguration configuration)
        {
            Configurator.Configure(configuration, _worldConfiguration);
        }

        public static void RegisterMapper<TGameObject, TDto>(IContainer container) where TGameObject : notnull
        {
            TypeAdapterConfig<TDto, TGameObject>.NewConfig().ConstructUsing(src => container.Resolve<TGameObject>());
        }

        public static void RegisterDatabaseObject<TDto, TDb, TPk>(ContainerBuilder containerBuilder, bool isStatic)
        where TDb : class where TPk : struct
        {
            containerBuilder.RegisterType<Dao<TDb, TDto, TPk>>().As<IDao<TDto, TPk>>().SingleInstance();
            if (!isStatic)
            {
                return;
            }

            var staticMetaDataAttribute = typeof(TDto).GetCustomAttribute<StaticMetaDataAttribute>();
            containerBuilder.Register(c =>
                {
                    var dic = c.Resolve<IDictionary<Type, Dictionary<string, Dictionary<RegionType, II18NDto>>>>();
                    var items = c.Resolve<IDao<TDto, TPk>>().LoadAll().ToList();
                    var props = StaticDtoExtension.GetI18NProperties(typeof(TDto));
                    if (props.Count > 0)
                    {
                        var regions = Enum.GetValues(typeof(RegionType));
                        var accessors = TypeAccessor.Create(typeof(TDto));
                        Parallel.ForEach(items, s => ((IStaticDto)s!).InjectI18N(props, dic, regions, accessors));
                    }

                    if ((items.Count != 0) || (staticMetaDataAttribute == null) ||
                        (staticMetaDataAttribute.EmptyMessage == LogLanguageKey.UNKNOWN))
                    {
                        if ((staticMetaDataAttribute != null) &&
                            (staticMetaDataAttribute.LoadedMessage != LogLanguageKey.UNKNOWN))
                        {
                            c.Resolve<ILogger>().Information(
                                LogLanguage.Instance.GetMessageFromKey(staticMetaDataAttribute.LoadedMessage),
                                items.Count);
                        }
                    }
                    else
                    {
                        c.Resolve<ILogger>()
                            .Error(LogLanguage.Instance.GetMessageFromKey(staticMetaDataAttribute.EmptyMessage));
                    }

                    return items;
                })
                .As<List<TDto>>()
                .SingleInstance()
                .AutoActivate();
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
                    assemblyGo.Where(t.IsAssignableFrom).ToList().ForEach(tgo =>
                    {
                        registerMapper?.MakeGenericMethod(tgo, t).Invoke(null, new object?[] { container });
                    });
                });
        }

        private static void RegisterDto(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(c =>
                {
                    var dic = new Dictionary<Type, Dictionary<string, Dictionary<RegionType, II18NDto>>>
                    {
                        {
                            typeof(I18NActDescDto),
                            c.Resolve<IDao<I18NActDescDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NBCardDto),
                            c.Resolve<IDao<I18NBCardDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NCardDto),
                            c.Resolve<IDao<I18NCardDto, int>>().LoadAll().GroupBy(x => x.Key ?? "").ToDictionary(x => x.Key,
                                x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NItemDto),
                            c.Resolve<IDao<I18NItemDto, int>>().LoadAll().GroupBy(x => x.Key ?? "").ToDictionary(x => x.Key,
                                x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NMapIdDataDto),
                            c.Resolve<IDao<I18NMapIdDataDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NMapPointDataDto),
                            c.Resolve<IDao<I18NMapPointDataDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NNpcMonsterDto),
                            c.Resolve<IDao<I18NNpcMonsterDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NNpcMonsterTalkDto),
                            c.Resolve<IDao<I18NNpcMonsterTalkDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NQuestDto),
                            c.Resolve<IDao<I18NQuestDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        },
                        {
                            typeof(I18NSkillDto),
                            c.Resolve<IDao<I18NSkillDto, int>>().LoadAll().GroupBy(x => x.Key ?? "")
                                .ToDictionary(x => x.Key,
                                    x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        }
                    };
                    return dic;
                })
                .AsImplementedInterfaces()
                .SingleInstance()
                .AutoActivate();

            var registerDatabaseObject = typeof(Startup).GetMethod(nameof(RegisterDatabaseObject));
            var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
            var assemblyDb = typeof(Account).Assembly.GetTypes();

            assemblyDto.Where(p =>
                    typeof(IDto).IsAssignableFrom(p) &&
                    (!p.Name.Contains("InstanceDto") || p.Name.Contains("Inventory")) && p.IsClass)
                .ToList()
                .ForEach(t =>
                {
                    var type = assemblyDb.First(tgo =>
                        string.Compare(t.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
                    var typepk = type.GetProperties()
                        .Where(s => _dataAccess.CreateContext().Model.FindEntityType(type)
                            .FindPrimaryKey().Properties.Select(x => x.Name)
                            .Contains(s.Name)
                        ).ToArray()[0];
                    registerDatabaseObject?.MakeGenericMethod(t, type, typepk!.PropertyType).Invoke(null,
                        new[] { containerBuilder, (object)typeof(IStaticDto).IsAssignableFrom(t) });
                });

            containerBuilder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>().As<IDao<IItemInstanceDto?, Guid>>().SingleInstance();
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register<IDbContextBuilder>(c => _dataAccess).AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<MapsterMapper.Mapper>().AsImplementedInterfaces().PropertiesAutowired();
            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => (p.Namespace != "NosCore.Packets.ServerPackets.Login") && (p.Name != "NoS0575Packet")
                    && p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList();
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
            containerBuilder.RegisterInstance(_worldConfiguration!).As<WorldConfiguration>().As<ServerConfiguration>();
            containerBuilder.RegisterInstance(_worldConfiguration!.MasterCommunication!).As<WebApiConfiguration>();
            containerBuilder.RegisterType<ChannelHttpClient>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.RegisterType<AuthHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ConnectedAccountHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterAssemblyTypes(typeof(BlacklistHttpClient).Assembly)
                .Where(t => t.Name.EndsWith("HttpClient"))
                .AsImplementedInterfaces()
                .PropertiesAutowired();
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });
            var keyByteArray = Encoding.Default.GetBytes(_worldConfiguration.MasterCommunication!.Password!.ToSha512());
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
            });
            containerBuilder.Register(c => new Channel
            {
                MasterCommunication = _worldConfiguration.MasterCommunication,
                ClientName = _worldConfiguration.ServerName!,
                ClientType = ServerType.WorldServer,
                ConnectedAccountLimit = _worldConfiguration.ConnectedAccountLimit,
                Port = _worldConfiguration.Port,
                ServerGroup = _worldConfiguration.ServerGroup,
                Host = _worldConfiguration.Host!,
                WebApi = _worldConfiguration.WebApi,
                Token = handler.WriteToken(securityToken)
            });
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
            containerBuilder.RegisterType<AuthController>().PropertiesAutowired();

            //NosCore.WorldServer
            containerBuilder.RegisterType<WorldServer>().PropertiesAutowired();

            //NosCore.GameObject
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
            TypeAdapterConfig.GlobalSettings.Default.IgnoreAttribute(typeof(I18NFromAttribute));
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult!);
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<OctileDistanceCalculator>().AsImplementedInterfaces();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();
            containerBuilder.RegisterAssemblyTypes(typeof(IInventoryService).Assembly, typeof(IExperienceService).Assembly)
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

            containerBuilder
                .RegisterAssemblyTypes(typeof(IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>).Assembly)
                .Where(t => typeof(IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>)
                    .IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder
                .RegisterAssemblyTypes(typeof(IEventHandler<MapItem, Tuple<MapItem, GetPacket>>).Assembly)
                .Where(t => typeof(IEventHandler<MapItem, Tuple<MapItem, GetPacket>>)
                    .IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder
                .RegisterAssemblyTypes(
                    typeof(IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>).Assembly)
                .Where(t => typeof(IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>)
                    .IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder
                .RegisterAssemblyTypes(typeof(IEventHandler<GuriPacket, GuriPacket>).Assembly)
                .Where(t => typeof(IEventHandler<GuriPacket, GuriPacket>)
                    .IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();
        }


        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try { Console.Title = Title; } catch (PlatformNotSupportedException) { }
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>()
                .UseNpgsql(_worldConfiguration.Database!.ConnectionString);
            _dataAccess = new DataAccessHelper();
            _dataAccess.Initialize(optionsBuilder.Options, Logger.GetLoggerConfiguration().CreateLogger());
            LogLanguage.Language = _worldConfiguration.Language;

            services.AddSwaggerGen(c =>
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NosCore World API", Version = "v1" }));

            services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
            services.AddHttpClient();
            var password = _worldConfiguration.MasterCommunication!.HashingType switch
            {
                HashingType.BCrypt => _worldConfiguration.MasterCommunication!.Password!.ToBcrypt(_worldConfiguration
                    .MasterCommunication!.Salt ?? ""),
                HashingType.Pbkdf2 => _worldConfiguration.MasterCommunication!.Password!.ToPbkdf2Hash(_worldConfiguration
                    .MasterCommunication!.Salt ?? ""),
                HashingType.Sha512 => _worldConfiguration.MasterCommunication!.Password!.ToSha512(),
                _ => _worldConfiguration.MasterCommunication!.Password!.ToSha512()
            };

            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.Default.GetBytes(password)),
                        ValidAudience = "Audience",
                        ValidIssuer = "Issuer",
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true
                    };
                });

            services.AddAuthorization(o =>
                {
                    o.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                });

            services.AddControllers()
            .AddApplicationPart(typeof(StatController).GetTypeInfo().Assembly)
            .AddApplicationPart(typeof(AuthController).GetTypeInfo().Assembly)
            .AddControllersAsServices();

            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

            TypeAdapterConfig.GlobalSettings
                .ForDestinationType<I18NString>()
                .BeforeMapping(s => s.Clear());
            TypeAdapterConfig.GlobalSettings.Default
                 .IgnoreMember((member, side) => ((side == MemberSide.Destination) && member.Type.GetInterfaces().Contains(typeof(IEntity))) || (member.Type.GetGenericArguments().Any() && member.Type.GetGenericArguments()[0].GetInterfaces().Contains(typeof(IEntity))));
            TypeAdapterConfig.GlobalSettings
                .When(s => !s.SourceType.IsAssignableFrom(s.DestinationType) && typeof(IStaticDto).IsAssignableFrom(s.DestinationType))
                .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>()
                .AfterMapping(dest => Task.Run(dest.InitializeAsync));
            TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
            var containerBuilder = new ContainerBuilder();
            InitializeContainer(containerBuilder);
            containerBuilder.Populate(services);
            var container = containerBuilder.Build();
            RegisterGo(container);

            Task.Run(container.Resolve<WorldServer>().RunAsync).Forget();

            return new AutofacServiceProvider(container);
        }


        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore World API"));
            app.UseAuthentication();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}