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
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject.Event;
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
using NosCore.Data.DataAttributes;
using NosCore.Data;
using NosCore.Data.I18N;
using NosCore.Database.Entities;
using Character = NosCore.GameObject.Character;
using CharacterRelation = NosCore.GameObject.CharacterRelation;
using Item = NosCore.GameObject.Providers.ItemProvider.Item.Item;
using Map = NosCore.GameObject.Map.Map;
using MapMonster = NosCore.GameObject.MapMonster;
using MapNpc = NosCore.GameObject.MapNpc;
using MapType = Mapster.MapType;
using Portal = NosCore.GameObject.Portal;
using Shop = NosCore.GameObject.Shop;
using ShopItem = NosCore.GameObject.ShopItem;

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
            Validator.ValidateObject(_worldConfiguration, new ValidationContext(_worldConfiguration),
                validateAllProperties: true);

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(_worldConfiguration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

            LogLanguage.Language = _worldConfiguration.Language;
        }

        private static void RegisterDatabaseObject<TGameObject, TDto>(ref ContainerBuilder containerBuilder)
        {
            var staticDtoAttribute = typeof(TDto).GetCustomAttribute<StaticDtoAttribute>();

            containerBuilder.Register(c =>
            {
                var items = c.Resolve<IGenericDao<TDto>>().LoadAll().Adapt<List<TGameObject>>().ToList();
                if (items.Count != 0 || staticDtoAttribute.EmptyMessage == LogLanguageKey.UNKNOWN)
                {
                    if (staticDtoAttribute.LoadedMessage != LogLanguageKey.UNKNOWN)
                    {
                        _logger.Information(LogLanguage.Instance.GetMessageFromKey(staticDtoAttribute.LoadedMessage),
                            items.Count);
                    }
                }
                else
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(staticDtoAttribute.EmptyMessage));
                }

                return items;
            }).As<List<TGameObject>>().SingleInstance();
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
            containerBuilder.RegisterType<GameObject.Character>().PropertiesAutowired();
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
            RegisterDao(ref containerBuilder);
            RegisterDatabaseObject<Item, ItemDto>(ref containerBuilder);
            RegisterDatabaseObject<NpcMonsterDto, NpcMonsterDto>(ref containerBuilder);
            RegisterDatabaseObject<ShopItemDto, ShopItemDto>(ref containerBuilder);
            RegisterDatabaseObject<ShopDto, ShopDto>(ref containerBuilder);
            RegisterDatabaseObject<Map, MapDto>(ref containerBuilder);
            RegisterDatabaseObject<MapMonsterDto, MapMonsterDto>(ref containerBuilder);
            RegisterDatabaseObject<MapNpcDto, MapNpcDto>(ref containerBuilder);

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

            containerBuilder
                .RegisterAssemblyTypes(
                    typeof(IHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>).Assembly)
                .Where(t => typeof(IHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>)
                    .IsAssignableFrom(t))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(IHandler<GuriPacket, GuriPacket>).Assembly)
                .Where(t => typeof(IHandler<GuriPacket, GuriPacket>).IsAssignableFrom(t))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();
        }

        private static void RegisterDao(ref ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<GenericDao<Database.Entities.Account, AccountDto>>().As<IGenericDao<AccountDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Character, CharacterDto>>().As<IGenericDao<CharacterDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Map, MapDto>>().As<IGenericDao<MapDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.MapNpc, MapNpcDto>>().As<IGenericDao<MapNpcDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.NpcMonster, NpcMonsterDto>>().As<IGenericDao<NpcMonsterDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Card, CardDto>>().As<IGenericDao<CardDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Drop, DropDto>>().As<IGenericDao<DropDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.BCard, BCardDto>>().As<IGenericDao<BCardDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Item, ItemDto>>().As<IGenericDao<ItemDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Quest, QuestDto>>().As<IGenericDao<QuestDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.QuestReward, QuestRewardDto>>().As<IGenericDao<QuestRewardDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.QuestObjective, QuestObjectiveDto>>().As<IGenericDao<QuestObjectiveDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Mate, MateDto>>().As<IGenericDao<MateDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.MapType, MapTypeDto>>().As<IGenericDao<MapTypeDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Portal, PortalDto>>().As<IGenericDao<PortalDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Combo, ComboDto>>().As<IGenericDao<ComboDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.RespawnMapType, RespawnMapTypeDto>>().As<IGenericDao<RespawnMapTypeDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.MapTypeMap, MapTypeMapDto>>().As<IGenericDao<MapTypeMapDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NActDesc, I18NActDescDto>>().As<IGenericDao<I18NActDescDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NCard, I18NCardDto>>().As<IGenericDao<I18NCardDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NBCard, I18NbCardDto>>().As<IGenericDao<I18NbCardDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NItem, I18NItemDto>>().As<IGenericDao<I18NItemDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NMapIdData, I18NMapIdDataDto>>().As<IGenericDao<I18NMapIdDataDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NMapPointData, I18NMapPointDataDto>>().As<IGenericDao<I18NMapPointDataDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NNpcMonster, I18NNpcMonsterDto>>().As<IGenericDao<I18NNpcMonsterDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NNpcMonsterTalk, I18NNpcMonsterTalkDto>>().As<IGenericDao<I18NNpcMonsterTalkDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NQuest, I18NQuestDto>>().As<IGenericDao<I18NQuestDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.I18NSkill, I18NSkillDto>>().As<IGenericDao<I18NSkillDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Skill, SkillDto>>().As<IGenericDao<SkillDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.NpcMonsterSkill, NpcMonsterSkillDto>>().As<IGenericDao<NpcMonsterSkillDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.MapMonster, MapMonsterDto>>().As<IGenericDao<MapMonsterDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>>().As<IGenericDao<CharacterRelationDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Family, FamilyDto>>().As<IGenericDao<FamilyDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.FamilyCharacter, FamilyCharacterDto>>().As<IGenericDao<FamilyCharacterDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.FamilyLog, FamilyLogDto>>().As<IGenericDao<FamilyLogDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.Shop, ShopDto>>().As<IGenericDao<ShopDto>>().SingleInstance();
            containerBuilder.RegisterType<GenericDao<Database.Entities.ShopItem, ShopItemDto>>().As<IGenericDao<ShopItemDto>>().SingleInstance();
            containerBuilder.RegisterType<ItemInstanceDao>().As<IGenericDao<IItemInstanceDto>>().SingleInstance();
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