using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNetty.Buffers;
using DotNetty.Codecs;
using JetBrains.Annotations;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Authorization;
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
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.I18N;
using NosCore.WorldServer.Controllers;
using Swashbuckle.AspNetCore.Swagger;

namespace NosCore.WorldServer
{
    public class Startup
    {
        private const string ConfigurationPath = @"../../../configuration";
        private const string Title = "NosCore - WorldServer";

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

        private ContainerBuilder InitializeContainer(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly).As<IPacketController>();
            containerBuilder.RegisterType<WorldDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<WorldEncoder>().As<MessageToMessageEncoder<string>>();
            containerBuilder.RegisterType<WorldServer>().PropertiesAutowired();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.Populate(services);
            return containerBuilder;
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            PrintHeader();
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
            var containerBuilder = InitializeContainer(services);
            containerBuilder.RegisterInstance(configuration).As<WorldConfiguration>().As<GameServerConfiguration>();
            containerBuilder.RegisterInstance(configuration.MasterCommunication).As<MasterCommunicationConfiguration>();
            var container = containerBuilder.Build();
            DependancyResolver.Init(new AutofacServiceProvider(container));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(WorldServer)));
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