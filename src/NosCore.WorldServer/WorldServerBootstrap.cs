﻿//  __  _  __    __   ___ __  ___ ___
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

using Autofac.Extensions.DependencyInjection;
using Autofac;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Core.Services.IdService;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Dao;
using NosCore.Database.Entities;
using NosCore.Database;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.Encoding.Filter;
using NosCore.Networking.Encoding;
using NosCore.Networking.SessionRef;
using NosCore.Networking;
using NosCore.PacketHandlers.Login;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AutofacSerilogIntegration;
using FastExpressionCompiler;
using Mapster;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi.Models;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Resource;
using NosCore.Database.Entities.Base;
using NosCore.GameObject;
using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Algorithm.ExperienceService;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.PathFinder.Heuristic;
using NosCore.PathFinder.Interfaces;
using System.Collections.Generic;
using FastMember;
using Microsoft.Extensions.Options;
using NosCore.Data.DataAttributes;
using NosCore.Data;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.Packets;
using ILogger = Serilog.ILogger;
using Character = NosCore.GameObject.Character;
using NosCore.Packets.Enumerations;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using NosCore.Networking.Extensions;
using System.Text;

namespace NosCore.WorldServer
{
    public static class WorldServerBootstrap
    {
        private const string Title = "NosCore - WorldServer";
        private const string ConsoleText = "WORLD SERVER - NosCoreIO";

        private static void InitializeConfiguration(string[] args, IServiceCollection services)
        {
            var worldConfiguration = new WorldConfiguration();
            var conf = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "world.yml" });
            conf.Bind(worldConfiguration);
            services.AddDbContext<NosCoreContext>(
                conf => conf.UseNpgsql(worldConfiguration.Database!.ConnectionString, options => { options.UseNodaTime(); }));
            services.AddOptions<WorldConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<ServerConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<WebApiConfiguration>().Bind(conf.GetSection(nameof(LoginConfiguration.MasterCommunication))).ValidateDataAnnotations();

            Logger.PrintHeader(ConsoleText);
            CultureInfo.DefaultThreadCurrentCulture = new(worldConfiguration.Language.ToString());
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
            containerBuilder.RegisterType<MapsterMapper.Mapper>().AsImplementedInterfaces();
            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && (p.GetCustomAttribute<PacketHeaderAttribute>() == null
                    || (p.GetCustomAttribute<PacketHeaderAttribute>()!.Scopes & Scope.OnLoginScreen) == 0) && p.IsClass && !p.IsAbstract).ToList();
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
            containerBuilder.RegisterAssemblyTypes(typeof(ChannelHubClient).Assembly)
                .Where(t => t.Name.EndsWith("HubClient") && t.Name != nameof(ChannelHubClient))
                .AsImplementedInterfaces();
            containerBuilder.RegisterType<ChannelHubClient>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<HubConnectionFactory>();

            containerBuilder.Register(c =>
            {
                var configuration = c.Resolve<IOptions<WorldConfiguration>>();
                return new Channel
                {
                    MasterCommunication = configuration.Value.MasterCommunication,
                    ClientName = configuration.Value.ServerName!,
                    ClientType = ServerType.WorldServer,
                    ConnectedAccountLimit = configuration.Value.ConnectedAccountLimit,
                    Port = configuration.Value.Port,
                    DisplayPort = configuration.Value.DisplayPort,
                    DisplayHost = configuration.Value.DisplayHost,
                    ServerId = configuration.Value.ServerId,
                    StartInMaintenance = configuration.Value.StartInMaintenance,
                    Host = configuration.Value.Host!,
                };
            });
            containerBuilder.Register<IHasher>(o => o.Resolve<IOptions<WebApiConfiguration>>().Value.HashingType switch
            {
                HashingType.BCrypt => new BcryptHasher(),
                HashingType.Pbkdf2 => new Pbkdf2Hasher(),
                _ => new Sha512Hasher()
            });

            //NosCore.Controllers
            containerBuilder.RegisterTypes(typeof(NoS0575PacketHandler).Assembly.GetTypes()
                    .Where(type => typeof(IPacketHandler).IsAssignableFrom(type) && typeof(IWorldPacketHandler).IsAssignableFrom(type)).ToArray())
                .AsImplementedInterfaces();

            //NosCore.Core
            containerBuilder.RegisterType<WorldDecoder>().AsImplementedInterfaces();
            containerBuilder.RegisterType<WorldEncoder>().AsImplementedInterfaces();
            containerBuilder.Register(x => new List<RequestFilter>()).As<IEnumerable<RequestFilter>>();
            containerBuilder.Register(_ => SystemClock.Instance).As<IClock>().SingleInstance();
            containerBuilder.RegisterType<ClientSession>().AsImplementedInterfaces();
            containerBuilder.RegisterType<SessionRefHolder>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>().AsImplementedInterfaces();
            containerBuilder.Register(_ => new PipelineConfiguration()).AsImplementedInterfaces();


            //NosCore.GameObject
            containerBuilder.RegisterType<OctileDistanceHeuristic>().As<IHeuristic>();
            containerBuilder.RegisterType<Clock>();
            containerBuilder.Register<IIdService<Group>>(_ => new IdService<Group>(1)).SingleInstance();
            containerBuilder.Register<IIdService<MapItem>>(_ => new IdService<MapItem>(100000)).SingleInstance();
            containerBuilder.Register<IIdService<ChannelInfo>>(_ => new IdService<ChannelInfo>(1)).SingleInstance();

            containerBuilder.RegisterAssemblyTypes(typeof(IInventoryService).Assembly, typeof(IExperienceService).Assembly)
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces();

            containerBuilder.RegisterAssemblyTypes(typeof(MapInstanceHolder).Assembly)
                .Where(t => t.Name.EndsWith("Holder"))
                .SingleInstance();

            RegisterDto(containerBuilder);

            containerBuilder.RegisterAssemblyTypes(typeof(Character).Assembly)
                .Where(t => typeof(IDto).IsAssignableFrom(t))
                .AsSelf();

            containerBuilder
                .RegisterGeneric(typeof(EventLoaderService<,,>));

            containerBuilder
                .RegisterAssemblyTypes(typeof(IEventHandler<,>).Assembly)
                .AsClosedTypesOf(typeof(IEventHandler<,>))
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder
                .RegisterAssemblyTypes(typeof(ChannelCommunicationMessageHandler<>).Assembly)
                .Where(t => typeof(IChannelCommunicationMessageHandler<IMessage>).IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();
        }


        public static void RegisterDatabaseObject<TDto, TDb, TPk>(ContainerBuilder containerBuilder, bool isStatic)
        where TDb : class
        where TPk : struct
        {
            containerBuilder.RegisterType<Dao<TDb, TDto, TPk>>().As<IDao<IDto>>().As<IDao<TDto, TPk>>().SingleInstance();
            if (!isStatic)
            {
                return;
            }

            var staticMetaDataAttribute = typeof(TDb).GetCustomAttribute<StaticMetaDataAttribute>();
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
                .As<List<TDto>>()
                .SingleInstance()
                .AutoActivate();
        }


        private static void RegisterDto(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(c => c.Resolve<IEnumerable<IDao<IDto>>>().OfType<IDao<II18NDto>>().ToDictionary(
                    x => x.GetType().GetGenericArguments()[1], y => y.LoadAll().GroupBy(x => x!.Key ?? "")
                        .ToDictionary(x => x.Key,
                            x => x.ToList().ToDictionary(o => o!.RegionType, o => o!))))
            .AsImplementedInterfaces()
            .SingleInstance()
            .AutoActivate();

            var registerDatabaseObject = typeof(WorldServerBootstrap).GetMethod(nameof(RegisterDatabaseObject));
            var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
            var assemblyDb = typeof(Account).Assembly.GetTypes();

            var assemblyGo = typeof(Character).Assembly.GetTypes();

            assemblyDto.Where(p => typeof(IDto).IsAssignableFrom(p) && p.IsClass)
                .ToList()
                .ForEach(t =>
                {
                    assemblyGo.Where(t.IsAssignableFrom).ToList().ForEach(tgo =>
                    {
                        containerBuilder.RegisterType(tgo);
                        containerBuilder
                            .RegisterType(typeof(GameObjectMapper<,>).MakeGenericType(t, tgo))
                            .As(typeof(IGameObjectMapper<>).MakeGenericType(t))
                            .AutoActivate();
                    });
                });

            assemblyDto.Where(p =>
                    typeof(IDto).IsAssignableFrom(p) &&
                    (!p.Name.Contains("InstanceDto") || p.Name.Contains("Inventory")) && p.IsClass)
                .ToList()
                .ForEach(t =>
                {
                    var type = assemblyDb.First(tgo =>
                        string.Compare(t.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
                    var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                        Guid.NewGuid().ToString());
                    var typepk = type.GetProperties()
                        .Where(s => new NosCoreContext(optionsBuilder.Options).Model.FindEntityType(type)?
                            .FindPrimaryKey()?.Properties.Select(x => x.Name)
                            .Contains(s.Name) ?? false
                        ).ToArray()[0];
                    registerDatabaseObject?.MakeGenericMethod(t, type, typepk!.PropertyType).Invoke(null,
                        new[] { containerBuilder, (object)typeof(IStaticDto).IsAssignableFrom(t) });
                });

            containerBuilder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>().As<IDao<IItemInstanceDto?, Guid>>().SingleInstance();
        }


        public static async Task Main(string[] args)
        {
            try
            {
                await BuildHost(args).RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static IHost BuildHost(string[] args)
        {
            return new HostBuilder()
                .UseSerilog()
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(InitializeContainer)
                .ConfigureServices((hostContext, services) =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.Title = Title;
                    }

                    InitializeConfiguration(args, services);

                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));

                    services.AddI18NLogs();
                    services.AddTransient(typeof(IGameLanguageLocalizer), typeof(GameLanguageLocalizer));
                    services.AddTransient(typeof(ILogLanguageLocalizer<LanguageKey>),
                        x => new LogLanguageLocalizer<LanguageKey, LocalizedResources>(
                            x.GetRequiredService<IStringLocalizer<LocalizedResources>>()));
                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.AddHostedService<WorldServer>();

                    TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
                    TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult!);
                    TypeAdapterConfig.GlobalSettings.ForDestinationType<I18NString>().BeforeMapping(s => s.Clear());
                    TypeAdapterConfig.GlobalSettings.Default.IgnoreMember((member, side)
                        => ((side == MemberSide.Destination) && member.Type.GetInterfaces().Contains(typeof(IEntity))) || (member.Type.GetGenericArguments().Any() && member.Type.GetGenericArguments()[0].GetInterfaces().Contains(typeof(IEntity))));
                    TypeAdapterConfig.GlobalSettings.When(s => !s.SourceType.IsAssignableFrom(s.DestinationType))
                        .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
                    TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
                    TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
                })
                .Build();
        }
    }

    public class WorldDecoder : MessageToMessageDecoder<IByteBuffer>, IDecoder
    {
        private readonly IDeserializer _deserializer;
        private readonly ILogger<WorldDecoder> _logger;
        private RegionType _region;
        private int _sessionId;
        private readonly ISessionRefHolder _sessionRefHolder;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public WorldDecoder(IDeserializer deserializer, ILogger<WorldDecoder> logger, ISessionRefHolder sessionRefHolder, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _deserializer = deserializer;
            _logger = logger;
            _sessionRefHolder = sessionRefHolder;
            _logLanguage = logLanguage;
        }

        private string DecryptPrivate(string str)
        {
            var receiveData = new List<byte>();
            char[] table = { ' ', '-', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'n' };
            int count;
            for (count = 0; count < str.Length; count++)
            {
                if (str[count] <= 0x7A)
                {
                    int len = str[count];

                    for (var i = 0; i < len; i++)
                    {
                        count++;

                        try
                        {
                            receiveData.Add(unchecked((byte)(str[count] ^ 0xFF)));
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
#pragma warning restore CA1031 // Do not catch general exception types
                        {
                            receiveData.Add(255);
                        }
                    }
                }
                else
                {
                    int len = str[count];
                    len &= 0x7F;

                    for (var i = 0; i < len; i++)
                    {
                        count++;
                        var highbyte = str.Length > count ? str[count] : 0;

                        highbyte &= 0xF0;
                        highbyte >>= 0x4;

                        var lowbyte = str.Length > count ? str[count] : 0;

                        lowbyte &= 0x0F;

                        if ((highbyte != 0x0) && (highbyte != 0xF))
                        {
                            receiveData.Add(unchecked((byte)table[highbyte - 1]));
                            i++;
                        }

                        if ((lowbyte != 0x0) && (lowbyte != 0xF))
                        {
                            receiveData.Add(unchecked((byte)table[lowbyte - 1]));
                        }
                    }
                }
            }

            return _region.GetEncoding()!.GetString(receiveData.ToArray());
        }

        private static string DecryptCustomParameter(byte[] str, out byte[] endOfPacket)
        {
            endOfPacket = Array.Empty<byte>();
            try
            {
                var encryptedStringBuilder = new StringBuilder();
                for (var i = 1; i < str.Length; i++)
                {
                    if (Convert.ToChar(str[i]) == 0xE)
                    {
                        endOfPacket = str.Skip(i + 1).ToArray();
                        return encryptedStringBuilder.ToString();
                    }

                    var firstbyte = Convert.ToInt32(str[i] - 0xF);
                    var secondbyte = firstbyte;
                    secondbyte &= 0xF0;
                    firstbyte = Convert.ToInt32(firstbyte - secondbyte);
                    secondbyte >>= 0x4;

                    switch (secondbyte)
                    {
                        case 0:
                        case 1:
                            encryptedStringBuilder.Append(' ');
                            break;

                        case 2:
                            encryptedStringBuilder.Append('-');
                            break;

                        case 3:
                            encryptedStringBuilder.Append('.');
                            break;

                        default:
                            secondbyte += 0x2C;
                            encryptedStringBuilder.Append(Convert.ToChar(secondbyte));
                            break;
                    }

                    switch (firstbyte)
                    {
                        case 0:

                        case 1:
                            encryptedStringBuilder.Append(' ');
                            break;

                        case 2:
                            encryptedStringBuilder.Append('-');
                            break;

                        case 3:
                            encryptedStringBuilder.Append('.');
                            break;

                        default:
                            firstbyte += 0x2C;
                            encryptedStringBuilder.Append(Convert.ToChar(firstbyte));
                            break;
                    }
                }

                return encryptedStringBuilder.ToString();
            }
            catch (OverflowException)
            {
                return string.Empty;
            }
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
        {
            var packets = Decode(context.Channel.Id.AsLongText(),
                ((Span<byte>)message.Array).Slice(message.ArrayOffset, message.ReadableBytes));

            if (packets.Any())
            {
                output.Add(packets);
            }
        }

        public IEnumerable<IPacket> Decode(string clientSessionId, Span<byte> message)
        {
            var continueToDecode = true;
            var temp = new List<IPacket>();
            var encryptedString = "";
            var mapper = _sessionRefHolder[clientSessionId];
            _region = mapper.RegionType;
            _sessionId = mapper.SessionId;
            var str = message.ToArray();
            if (_sessionId == 0)
            {
                if (_deserializer.Deserialize(DecryptCustomParameter(str, out var endofPacket)) is not UnresolvedPacket pack)
                {
                    throw new ArgumentNullException(nameof(clientSessionId));
                }

                if (!int.TryParse(pack.Header, out _sessionId))
                {
                    //_logger.LogError(_logLanguage[LogLanguageKey.ERROR_SESSIONID], mapper.SessionId);
                    return Enumerable.Empty<IPacket>();
                }

                _sessionRefHolder[clientSessionId].SessionId = _sessionId;
                //_logger.LogInformation(_logLanguage[LogLanguageKey.CLIENT_CONNECTED], mapper.SessionId);
                temp.Add(pack);
                if (endofPacket.Length == 0)
                {
                    continueToDecode = false;
                }

                str = endofPacket;
            }

            _logger.LogInformation($"last byte (delimiter) expected {(mapper.SessionId == 0 ? (byte)0xE : unchecked((byte)(63 + mapper.SessionId)))}");
            _logger.LogInformation($"last byte (delimiter) received {str.Last()}");

            if (continueToDecode)
            {
                var sessionKey = _sessionId & 0xFF;
                var sessionNumber = unchecked((byte)(_sessionId >> 6));
                sessionNumber &= 0xFF;
                sessionNumber &= unchecked((byte)0x80000003);

                switch (sessionNumber)
                {
                    case 0:
                        encryptedString =
                            (from character in str
                             let firstbyte = unchecked((byte)(sessionKey + 0x40))
                             select unchecked((byte)(character - firstbyte))).Aggregate(encryptedString,
                                (current, highbyte) => current + (char)highbyte);
                        break;

                    case 1:
                        encryptedString =
                            (from character in str
                             let firstbyte = unchecked((byte)(sessionKey + 0x40))
                             select unchecked((byte)(character + firstbyte))).Aggregate(encryptedString,
                                (current, highbyte) => current + (char)highbyte);
                        break;

                    case 2:
                        encryptedString =
                            (from character in str
                             let firstbyte = unchecked((byte)(sessionKey + 0x40))
                             select unchecked((byte)((character - firstbyte) ^ 0xC3))).Aggregate(encryptedString,
                                (current, highbyte) => current + (char)highbyte);
                        break;

                    case 3:
                        encryptedString =
                            (from character in str
                             let firstbyte = unchecked((byte)(sessionKey + 0x40))
                             select unchecked((byte)((character + firstbyte) ^ 0xC3))).Aggregate(encryptedString,
                                (current, highbyte) => current + (char)highbyte);
                        break;

                    default:
                        encryptedString += (char)0xF;
                        break;
                }

                temp.AddRange(encryptedString.Split((char)0xFF, StringSplitOptions.RemoveEmptyEntries).Select(p =>
                {
                    try
                    {
                        var decrypt = DecryptPrivate(p);
                        var packet = _deserializer.Deserialize(decrypt);
                        if (!packet.IsValid)
                        {
                            //_logger.LogError(_logLanguage[LogLanguageKey.CORRUPTED_PACKET],
                            //    packet.Header, decrypt);
                        }

                        return packet;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        //_logger.LogError(_logLanguage[LogLanguageKey.ERROR_DECODING],
                        //    ex.Data["Packet"]);
                        ushort? keepalive = null;
                        if (ushort.TryParse(ex.Data["Packet"]?.ToString()?.Split(" ")[0], out var kpalive))
                        {
                            keepalive = kpalive;
                        }
                        return new UnresolvedPacket
                        { KeepAliveId = keepalive, Header = "0" };
                    }
                }));
            }

            return temp;
        }
    }

}
