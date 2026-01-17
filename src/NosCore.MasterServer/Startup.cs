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
using NodaTime;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Services.IdService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.DataAttributes;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Services.BazaarService;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ConfigureJwtBearerOptions = NosCore.Core.ConfigureJwtBearerOptions;
using ILogger = Serilog.ILogger;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.AuthService;

namespace NosCore.MasterServer
{
    public class Startup(IConfiguration configuration)
    {
        private const string Title = "NosCore - MasterServer";
        private const string ConsoleText = "MASTER SERVER - NosCoreIO";

        private static void RegisterDto(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(c => c.Resolve<IEnumerable<IDao<IDto>>>().OfType<IDao<II18NDto>>().ToDictionary(
                    x => x.GetType().GetGenericArguments()[1], y => y.LoadAll().GroupBy(x => x!.Key ?? "")
                        .ToDictionary(x => x.Key,
                            x => x.ToList().ToDictionary(o => o!.RegionType, o => o!))))
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
                        .Where(s => new NosCoreContext(new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                            Guid.NewGuid().ToString()).Options).Model.FindEntityType(type)?
                            .FindPrimaryKey()?.Properties.Select(x => x.Name)
                            .Contains(s.Name) ?? false
                        ).ToArray()[0];
                    registerDatabaseObject?.MakeGenericMethod(t, type, typepk!.PropertyType)
                        .Invoke(null, new object?[] { containerBuilder });
                });


            containerBuilder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>().As<IDao<IItemInstanceDto?, Guid>>().SingleInstance();

            containerBuilder.Register(c =>
            {
                var dic = c.Resolve<IDictionary<Type, Dictionary<string, Dictionary<RegionType, II18NDto>>>>();
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
                        c.Resolve<ILogger>().Information(c.Resolve<ILogLanguageLocalizer<LogLanguageKey>>()[staticMetaDataAttribute.LoadedMessage],
                            items.Count);
                    }
                }
                else
                {
                    c.Resolve<ILogger>()
                        .Error(c.Resolve<ILogLanguageLocalizer<LogLanguageKey>>()[staticMetaDataAttribute.EmptyMessage]);
                }

                return items;
            })
                .As<List<ItemDto>>()
                .SingleInstance()
                .AutoActivate();
        }

        public static void RegisterDatabaseObject<TDto, TDb, TPk>(ContainerBuilder containerBuilder) where TDb : class where TPk : struct
        {
            containerBuilder.RegisterType<Dao<TDb, TDto, TPk>>().As<IDao<TDto, TPk>>().As<IDao<IDto>>().SingleInstance();
        }

        private ContainerBuilder InitializeContainer(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<ChannelHub>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<MasterClientList>().SingleInstance();
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
                    Host = "",
                    MasterCommunication = configuration.Value.WebApi,
                    ClientName = "Master Server",
                    ClientType = ServerType.MasterServer,
                };
            });
            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
            containerBuilder.Register<IIdService<ChannelInfo>>(_ => new IdService<ChannelInfo>(1)).SingleInstance();
            containerBuilder.RegisterLogger();

            containerBuilder.Register(_ => SystemClock.Instance).As<IClock>().SingleInstance();
            containerBuilder.RegisterAssemblyTypes(typeof(FriendRequestHolder).Assembly)
                .Where(t => t.Name.EndsWith("Holder"))
                .SingleInstance();

            containerBuilder.RegisterType<ChannelHub>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.RegisterType<AuthCodeService>().As<IAuthCodeService>().SingleInstance();
            containerBuilder.RegisterAssemblyTypes(typeof(ChannelHub).Assembly)
                .Where(t => t.Name.EndsWith("Hub") && t.Name != nameof(ChannelHub))
                .AsImplementedInterfaces();
            containerBuilder.RegisterAssemblyTypes(typeof(BazaarService).Assembly)
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces();

            containerBuilder
                .RegisterGeneric(typeof(EventLoaderService<,,>));

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
            services.AddOptions<MasterConfiguration>().Bind(configuration).ValidateDataAnnotations();
            services.AddOptions<WebApiConfiguration>().Bind(configuration.GetSection(nameof(MasterConfiguration.WebApi))).ValidateDataAnnotations();
            services.AddI18NLogs();

            var masterConfiguration = new MasterConfiguration();
            configuration.Bind(masterConfiguration);

            services.Configure<KestrelServerOptions>(options => options.ListenAnyIP(masterConfiguration.WebApi.Port));
            services.AddDbContext<NosCoreContext>(
                conf => conf.UseNpgsql(masterConfiguration.Database!.ConnectionString, options => { options.UseNodaTime(); }));
            services.ConfigureOptions<ConfigureJwtBearerOptions>();
           
            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            services.AddSignalR(options =>
            {
                options.DisableImplicitFromServicesParameters = true;
            }).AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new PolymorphicJsonConverter<IMessage>());
            });
            services.AddAuthorization(o =>
                {
                    o.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                });

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
            app.UseAuthentication();
            app.UseRouting();

            app.UseWebSockets();
            app.UseAuthorization();
            CultureInfo.DefaultThreadCurrentCulture = new(app.ApplicationServices.GetRequiredService<IOptions<MasterConfiguration>>().Value.Language.ToString());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<AuthHub>(nameof(AuthHub));
                endpoints.MapHub<PubSubHub>(nameof(PubSubHub));
                endpoints.MapHub<BazaarHub>(nameof(BazaarHub));
                endpoints.MapHub<BlacklistHub>(nameof(BlacklistHub));
                endpoints.MapHub<ChannelHub>(nameof(ChannelHub));
                endpoints.MapHub<FriendHub>(nameof(FriendHub));
                endpoints.MapHub<MailHub>(nameof(MailHub));
                endpoints.MapHub<WarehouseHub>(nameof(WarehouseHub));
            });
        }
    }
}
