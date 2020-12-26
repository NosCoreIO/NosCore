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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using FastExpressionCompiler;
using FastMember;
using JetBrains.Annotations;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.HttpClients.IncommingMailHttpClients;
using NosCore.Core.I18N;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.DataAttributes;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using ILogger = Serilog.ILogger;
using NosCore.Shared.I18N;
using ConfigureJwtBearerOptions = NosCore.Core.ConfigureJwtBearerOptions;

namespace NosCore.MasterServer
{
    public class Startup
    {
        private const string Title = "NosCore - MasterServer";
        private const string ConsoleText = "MASTER SERVER - NosCoreIO";
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private static void RegisterDto(ContainerBuilder containerBuilder)
        {
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
                        .Where(s => new NosCoreContext(new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                            Guid.NewGuid().ToString()).Options).Model.FindEntityType(type)
                            .FindPrimaryKey().Properties.Select(x => x.Name)
                            .Contains(s.Name)
                        ).ToArray()[0];
                    registerDatabaseObject?.MakeGenericMethod(t, type, typepk!.PropertyType)
                        .Invoke(null, new object?[] { containerBuilder });
                });


            containerBuilder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>().As<IDao<IItemInstanceDto?, Guid>>().SingleInstance();

            containerBuilder.Register(c =>
                {
                    var dic = new Dictionary<Type, Dictionary<string, Dictionary<RegionType, II18NDto>>>
                    {
                        {
                            typeof(I18NItemDto),
                            c.Resolve<IDao<I18NItemDto, int>>().LoadAll().GroupBy(x => x.Key).ToDictionary(x => x.Key ?? "",
                                x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto) o))
                        }
                    };

                    var items = c.Resolve<IDao<ItemDto, short>>().LoadAll().ToList();
                    var props = StaticDtoExtension.GetI18NProperties(typeof(ItemDto));

                    var regions = Enum.GetValues(typeof(RegionType));
                    var accessors = TypeAccessor.Create(typeof(ItemDto));
                    Parallel.ForEach(items, s => s.InjectI18N(props, dic, regions, accessors));
                    var staticMetaDataAttribute = typeof(ItemDto).GetCustomAttribute<StaticMetaDataAttribute>();
                    if ((items.Count != 0) || (staticMetaDataAttribute == null) ||
                        (staticMetaDataAttribute.EmptyMessage == LogLanguageKey.UNKNOWN))
                    {
                        if ((items.Count != 0) && (staticMetaDataAttribute != null))
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
                .As<List<ItemDto>>()
                .SingleInstance()
                .AutoActivate();
        }

        public static void RegisterDatabaseObject<TDto, TDb, TPk>(ContainerBuilder containerBuilder) where TDb : class where TPk : struct
        {
            containerBuilder.RegisterType<Dao<TDb, TDto, TPk>>().As<IDao<TDto, TPk>>().SingleInstance();
        }

        private ContainerBuilder InitializeContainer(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.Register<IHasher>(o => o.Resolve<IOptions<WebApiConfiguration>>().Value.HashingType switch
            {
                HashingType.BCrypt => new BcryptHasher(),
                HashingType.Pbkdf2 => new Pbkdf2Hasher(),
                _ => new Sha512Hasher()
            });
            containerBuilder.Register(c =>
            {
                var configuration = c.Resolve<IOptions<MasterConfiguration>>();
                return new Channel
                {
                    MasterCommunication = configuration.Value.WebApi,
                    ClientName = "Master Server",
                    ClientType = ServerType.MasterServer,
                    WebApi = configuration.Value.WebApi
                };
            });
            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
            containerBuilder.RegisterType<AuthController>().PropertiesAutowired();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterType<FriendRequestHolder>().SingleInstance();
            containerBuilder.RegisterType<BazaarItemsHolder>().SingleInstance();
            containerBuilder.RegisterType<ParcelHolder>().SingleInstance();
            containerBuilder.RegisterType<ChannelHttpClient>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.RegisterType<ConnectedAccountHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<IncommingMailHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ItemProvider>().AsImplementedInterfaces();
            containerBuilder.Populate(services);
            RegisterDto(containerBuilder);
            return containerBuilder;
        }


        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title = Title;
            }
            Logger.PrintHeader(ConsoleText);
            services.AddOptions<MasterConfiguration>().Bind(_configuration).ValidateDataAnnotations();
            services.AddOptions<WebApiConfiguration>().Bind(_configuration.GetSection(nameof(MasterConfiguration.WebApi))).ValidateDataAnnotations();

            var masterConfiguration = new MasterConfiguration();
            _configuration.Bind(masterConfiguration);

            services.Configure<KestrelServerOptions>(options => options.ListenAnyIP(masterConfiguration.WebApi.Port));
            services.AddDbContext<NosCoreContext>(
                conf => conf.UseNpgsql(masterConfiguration.Database!.ConnectionString));
            services.AddSwaggerGen(c =>
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NosCore Master API", Version = "v1" }));

            services.ConfigureOptions<ConfigureJwtBearerOptions>();
            services.AddHttpClient();
            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            services.AddAuthorization(o =>
                {
                    o.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                });

            services
                .AddControllers()
                .AddApplicationPart(typeof(AuthController).GetTypeInfo().Assembly)
                .AddApplicationPart(typeof(FriendController).GetTypeInfo().Assembly)
                .AddControllersAsServices();
            services.AddHostedService<MasterServer>();
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IStaticDto>()
                .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
            TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();

            var containerBuilder = InitializeContainer(services);
            var container = containerBuilder.Build();
            return new AutofacServiceProvider(container);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore Master API"));
            app.UseAuthentication();
            app.UseRouting();

            app.UseAuthorization();

            LogLanguage.Language = app.ApplicationServices.GetRequiredService<IOptions<MasterConfiguration>>().Value.Language;
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
