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
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations;
using NosCore.Data.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace NosCore.MasterServer
{
    public class Startup
    {
        private MasterConfiguration _configuration;
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - MasterServer";
        private const string ConsoleText = "MASTER SERVER - NosCoreIO";

        private MasterConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var masterConfiguration = new MasterConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("master.json", false);
            builder.Build().Bind(masterConfiguration);
            Validator.ValidateObject(masterConfiguration, new ValidationContext(masterConfiguration),
                validateAllProperties: true);
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(masterConfiguration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

            return masterConfiguration;
        }

        private static void RegisterDto(ContainerBuilder containerBuilder)
        {
            var registerDatabaseObject = typeof(Startup).GetMethod(nameof(RegisterDatabaseObject));
            var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
            var assemblyDb = typeof(Database.Entities.Account).Assembly.GetTypes();

            assemblyDto.Where(p => typeof(IDto).IsAssignableFrom(p) && (!p.Name.Contains("InstanceDto") || p.Name.Contains("Inventory")) && p.IsClass)
                .ToList()
                .ForEach(t =>
                {
                    var type = assemblyDb.First(tgo =>
                        string.Compare(t.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
                    registerDatabaseObject.MakeGenericMethod(t, type).Invoke(null, new[] { containerBuilder });
                });

            containerBuilder.RegisterType<ItemInstanceDao>().As<IGenericDao<IItemInstanceDto>>().SingleInstance();

            containerBuilder.Register(c =>
                {
                    var dic = new Dictionary<Type, Dictionary<string, Dictionary<RegionType, II18NDto>>>
                    {
                        { typeof(I18NItemDto), c.Resolve<IGenericDao<I18NItemDto>>().LoadAll().GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList().ToDictionary(o => o.RegionType, o => (II18NDto)o)) },
                    };

                    var items = c.Resolve<IGenericDao<ItemDto>>().LoadAll().ToList();
                    var props = StaticDtoExtension.GetI18NProperties(typeof(ItemDto));

                    var regions = Enum.GetValues(typeof(RegionType));
                    var accessors = TypeAccessor.Create(typeof(ItemDto));
                    Parallel.ForEach(items, (s) => (s).InjectI18N(props, dic, regions, accessors));
                    StaticMetaDataAttribute staticMetaDataAttribute = typeof(ItemDto).GetCustomAttribute<StaticMetaDataAttribute>();
                    if (items.Count != 0)
                    {
                        c.Resolve<Serilog.ILogger>().Information(LogLanguage.Instance.GetMessageFromKey(staticMetaDataAttribute.LoadedMessage),
                            items.Count);
                    }
                    else
                    {
                        c.Resolve<Serilog.ILogger>().Error(LogLanguage.Instance.GetMessageFromKey(staticMetaDataAttribute.EmptyMessage));
                    }

                    return items;
                })
                .As<List<ItemDto>>()
                .SingleInstance()
                .AutoActivate();
        }

        public static void RegisterDatabaseObject<TDto, TDb>(ContainerBuilder containerBuilder) where TDb : class
        {
            containerBuilder.RegisterType<GenericDao<TDb, TDto>>().As<IGenericDao<TDto>>().SingleInstance();
        }

        private ContainerBuilder InitializeContainer(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<MasterServer>().PropertiesAutowired();
            containerBuilder.Register(c => new Channel
            {
                MasterCommunication = _configuration.WebApi,
                ClientName = "Master Server",
                ClientType = ServerType.MasterServer,
                WebApi = _configuration.WebApi,
            });
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
            Console.Title = Title;
            Logger.PrintHeader(ConsoleText);
            _configuration = InitializeConfiguration();
            services.AddSingleton<IServerAddressesFeature>(new ServerAddressesFeature
            {
                PreferHostingUrls = true,
                Addresses = { _configuration.WebApi.ToString() }
            });
            LogLanguage.Language = _configuration.Language;
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "NosCore Master API", Version = "v1" }));
            string password;
            switch (_configuration.WebApi.HashingType)
            {
                case HashingType.BCrypt:
                    password = _configuration.WebApi.Password.ToBcrypt(_configuration.WebApi.Salt);
                    break;
                case HashingType.Pbkdf2:
                    password = _configuration.WebApi.Password.ToPbkdf2Hash(_configuration.WebApi.Salt);
                    break;
                case HashingType.Sha512:
                default:
                    password = _configuration.WebApi.Password.ToSha512();
                    break;
            }
            var keyByteArray = Encoding.Default.GetBytes(password);
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            services.AddHttpClient();
            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
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
                    o.EnableEndpointRouting = false;
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    o.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddNewtonsoftJson()
                .AddApplicationPart(typeof(AuthController).GetTypeInfo().Assembly)
                .AddApplicationPart(typeof(FriendController).GetTypeInfo().Assembly)
                .AddControllersAsServices();

            TypeAdapterConfig.GlobalSettings.ForDestinationType<IStaticDto>()
                .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();

            var containerBuilder = InitializeContainer(services);
            containerBuilder.RegisterInstance(_configuration).As<MasterConfiguration>();
            containerBuilder.RegisterInstance(_configuration.WebApi).As<WebApiConfiguration>();
            var container = containerBuilder.Build();
            Task.Run(() => container.Resolve<MasterServer>().Run());
            return new AutofacServiceProvider(container);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore Master API"));
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}