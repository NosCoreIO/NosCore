//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.Observability;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.Messaging;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System.Net;

namespace NosCore.WebApi
{
    public class WebApiBootstrap
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddRazorPages();
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<NosCoreContext>("database");
            builder.Services.AddNosCoreTelemetry("NosCore.WebApi", includeAspNetCore: true);
            var loginConfiguration = new ApiConfiguration();
            var conf = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "api.yml" });
            conf.Bind(loginConfiguration);
            RequiredSecrets.EnsureSet(conf, RequiredSecrets.MasterPassword(nameof(ApiConfiguration.MasterCommunication)),
                RequiredSecrets.DatabasePassword, RequiredSecrets.JwtSigningKey, RequiredSecrets.MallSignatureKey);
            builder.Services.AddDbContext<NosCoreContext>(
                conf => conf.UseNpgsql(loginConfiguration.Database.ConnectionString, options => { options.UseNodaTime(); }));
            builder.Services.AddOptions<LoginConfiguration>().Bind(conf).ValidateDataAnnotations();
            builder.Services.AddOptions<ServerConfiguration>().Bind(conf).ValidateDataAnnotations();
            builder.Services.AddOptions<WebApiConfiguration>().Bind(conf.GetSection(nameof(ApiConfiguration.MasterCommunication))).ValidateDataAnnotations();
            builder.Services.AddOptions<JwtConfiguration>().Bind(conf.GetSection("Jwt")).ValidateDataAnnotations();
            builder.Services.AddOptions<MallConfiguration>().Bind(conf.GetSection("Mall")).ValidateDataAnnotations();
            builder.Services.Configure<ForwardedHeadersOptions>(options => ConfigureForwardedHeaders(options,
                conf.GetSection("ForwardedHeaders").Get<ForwardedHeadersConfiguration>() ?? new ForwardedHeadersConfiguration()));
            builder.Services.AddI18NLogs();

            builder.Services.ConfigureOptions<NosCore.Core.ConfigureJwtBearerOptions>();
            builder.Services.AddAuthentication(c => c.DefaultScheme = JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            builder.Services.AddAuthorization();

            builder.Host.UseNosCoreWolverine(conf,
                InterChannelNode.For("NosCore.WebApi", "webapi", loginConfiguration.Host, loginConfiguration.Port),
                opts => opts.Discovery.DisableConventionalDiscovery());
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(
                containerBuilder =>
                {
                    // Talk to the shared auth-code store on MasterServer via the
                    // AuthHub client rather than using a local AuthCodeService —
                    // otherwise codes stored here never reach the LoginServer
                    // process that has to validate them.
                    containerBuilder.RegisterType<AuthHubClient>().AsImplementedInterfaces();
                    containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
                    containerBuilder.RegisterType<Dao<Account, AccountDto, long>>().As<IDao<AccountDto, long>>().SingleInstance();

                    containerBuilder.Register<IHasher>(o => o.Resolve<IOptions<WebApiConfiguration>>().Value.HashingType switch
                    {
                        HashingType.BCrypt => new BcryptHasher(),
                        HashingType.Pbkdf2 => new Pbkdf2Hasher(),
                        _ => new Sha512Hasher()
                    });
                });

            var app = builder.Build();
            app.UseForwardedHeaders();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();
            app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
            app.MapHealthChecks("/health/ready").AllowAnonymous();
            app.Run();
        }

        public static void ConfigureForwardedHeaders(ForwardedHeadersOptions options, ForwardedHeadersConfiguration configuration)
        {
            // With no known proxy the middleware would trust every sender, so the header is not read at all.
            options.ForwardedHeaders = configuration.KnownProxies.Count + configuration.KnownNetworks.Count == 0
                ? ForwardedHeaders.None
                : ForwardedHeaders.XForwardedFor;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var proxy in configuration.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in configuration.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        }
    }
}
