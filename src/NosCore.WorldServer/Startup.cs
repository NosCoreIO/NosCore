using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using DotNetty.Buffers;
using DotNetty.Codecs;
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
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.DAL;
using NosCore.Data;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Item;
using NosCore.GameObject.Map;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using NosCore.WorldServer.Controllers;
using Swashbuckle.AspNetCore.Swagger;

namespace NosCore.WorldServer
{
    public class Startup
    {
        private const string ConfigurationPath = @"../../../configuration";
        private const string Title = "NosCore - WorldServer";
        private int npccount;
        private int monstercount;
        private IMapper _mapper;
        private void PrintHeader()
        {
            Console.Title = Title;
            const string text = "WORLD SERVER - 0Lucifer0";
            var offset = Console.WindowWidth / 2 + text.Length / 2;
            var separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        private WorldConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var worldConfiguration = new WorldConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("world.json", false);
            builder.Build().Bind(worldConfiguration);
            return worldConfiguration;
        }

        private void InitializeContainer(ref ContainerBuilder containerBuilder, IServiceCollection services)
        {
            containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly).As<IPacketController>();
            containerBuilder.RegisterType<WorldDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<WorldEncoder>().As<MessageToMessageEncoder<string>>();
            containerBuilder.RegisterType<WorldServer>().PropertiesAutowired();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<Portal>();
            containerBuilder.RegisterType<Inventory>();
            containerBuilder.RegisterType<PipelineFactory>();
            containerBuilder.RegisterType<ConcurrentDictionary<Guid, MapInstance>>();
            containerBuilder.Register(_ =>
            {
                var items = DAOFactory.ItemDAO.LoadAll().Adapt<List<Item>>().ToList();
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ITEMS_LOADED), items.Count));
                return items;
            }).As<List<Item>>().SingleInstance();

            containerBuilder.Register(_ =>
            {
                var monsters = DAOFactory.NpcMonsterDAO.LoadAll().ToList();
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NPCMONSTERS_LOADED), monsters.Count));
                return monsters;
            }).As<List<NpcMonsterDTO>>().SingleInstance();

            containerBuilder.Register(c => LoadMapInstances(mapInstances: c.Resolve<ConcurrentDictionary<Guid, MapInstance>>(), npcMonsters: c.Resolve<List<NpcMonsterDTO>>())).As<List<Map>>().SingleInstance();

            containerBuilder.Populate(services);
        }

        public List<Map> LoadMapInstances(ConcurrentDictionary<Guid, MapInstance> mapInstances, List<NpcMonsterDTO> npcMonsters)
        {
            var Maps = new List<Map>();
            var mapcount = 0;
            var mapPartitioner = Partitioner.Create(DAOFactory.MapDAO.LoadAll().Adapt<List<Map>>(),
                EnumerablePartitionerOptions.NoBuffering);
            var mapList = new ConcurrentDictionary<short, Map>();
            Parallel.ForEach(mapPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, map =>
            {
                var guid = Guid.NewGuid();
                map.Initialize();
                mapList[map.MapId] = map;
                var newMap = new MapInstance(map, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance, npcMonsters);
                mapInstances.TryAdd(guid, newMap);
                newMap.LoadPortals();
                newMap.LoadMonsters();
                newMap.LoadNpcs();
                newMap.StartLife();
                monstercount += newMap.Monsters.Count;
                npccount += newMap.Npcs.Count;
                mapcount++;
            });
            Maps.AddRange(mapList.Select(s => s.Value));
            if (mapcount != 0)
            {
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPS_LOADED), mapcount));
            }
            else
            {
                Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NO_MAP));
            }
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPNPCS_LOADED),
                npccount));
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPMONSTERS_LOADED),
                monstercount));
            return Maps;
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            PrintHeader();
            PacketFactory.Initialize<NoS0575Packet>();
            var configuration = InitializeConfiguration();
            Logger.InitializeLogger(LogManager.GetLogger(typeof(WorldServer)));
            DataAccessHelper.Instance.Initialize(configuration.Database);

            services.AddSingleton<IServerAddressesFeature>(new ServerAddressesFeature
            {
                PreferHostingUrls = true,
                Addresses = { configuration.WebApi.ToString() }
            });
            LogLanguage.Language = configuration.Language;
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info { Title = "NosCore World API", Version = "v1" }));
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
            services.AddSingleton(_ => _mapper);
            InitializeContainer(ref containerBuilder, services);
            var container = containerBuilder.Build();
            optionsBuilder.UseNpgsql(configuration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);
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