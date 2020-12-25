using System;
using System.Runtime.InteropServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.I18N;

namespace NosCore.WebApi
{
    public class Startup
    {
        private const string Title = "NosCore - WebApi";
        private const string ConsoleText = "WEBAPI - NosCoreIO";

        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title = Title;
            }
            Logger.PrintHeader(ConsoleText);

            services.AddOptions<WebApiConfiguration>().Bind(_configuration.GetSection(nameof(_configuration))).ValidateDataAnnotations();

            var webApiConfiguration = new WebApiConfiguration();
            _configuration.Bind(webApiConfiguration);

            services.Configure<KestrelServerOptions>(options => options.ListenAnyIP(webApiConfiguration.Port));

            services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
            services.AddHttpClient();
            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.ConfigureOptions<ConfigureJwtBearerOptions>();
            services.AddAuthorization(o =>
            {
                o.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services
                .AddControllers()
                .AddControllersAsServices();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(services);
            var container = containerBuilder.Build();
            return new AutofacServiceProvider(container);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore Web API"));
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
