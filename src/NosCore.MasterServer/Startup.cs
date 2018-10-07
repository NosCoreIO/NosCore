using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastExpressionCompiler;
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
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.DAL;
using NosCore.Database;
using NosCore.MasterServer.Controllers;
using NosCore.Shared.I18N;
using Swashbuckle.AspNetCore.Swagger;

namespace NosCore.MasterServer
{
    public class Startup
    {
        private const string ConfigurationPath = @"../../../configuration";
        private const string Title = "NosCore - MasterServer";

        private void PrintHeader()
        {
            Console.Title = Title;
            const string text = "MASTER SERVER - 0Lucifer0";
            var offset = Console.WindowWidth / 2 + text.Length / 2;
            var separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        private MasterConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var masterConfiguration = new MasterConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("master.json", false);
            builder.Build().Bind(masterConfiguration);
            return masterConfiguration;
        }

        private ContainerBuilder InitializeContainer(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<MasterServer>().PropertiesAutowired();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();
            containerBuilder.Populate(services);
            return containerBuilder;
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            PrintHeader();
            var configuration = InitializeConfiguration();
            services.AddSingleton<IServerAddressesFeature>(new ServerAddressesFeature
            {
                PreferHostingUrls = true,
                Addresses = {configuration.WebApi.ToString()}
            });
            LogLanguage.Language = configuration.Language;
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info {Title = "NosCore Master API", Version = "v1"}));
            var keyByteArray = Encoding.Default.GetBytes(EncryptionHelper.Sha512(configuration.Password));
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
            containerBuilder.RegisterInstance(configuration).As<MasterConfiguration>().As<WebApiConfiguration>();
            var container = containerBuilder.Build();
            Logger.InitializeLogger(LogManager.GetLogger(typeof(MasterServer)));
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(configuration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
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