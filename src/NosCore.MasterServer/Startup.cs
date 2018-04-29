using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NosCore.Configuration;
using NosCore.Core.Encryption;
using NosCore.WebApi.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.MasterServer
{
    public class Startup
    {
        private const string _configurationPath = @"..\..\..\configuration";
        private void PrintHeader()
        {
            Console.Title = "NosCore - MasterServer";
            const string text = "MASTER SERVER - 0Lucifer0";
            int offset = (Console.WindowWidth / 2) + (text.Length / 2);
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        private MasterConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            MasterConfiguration masterConfiguration = new MasterConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("master.json", false);
            builder.Build().Bind(masterConfiguration);
            return masterConfiguration;
        }

        private ContainerBuilder InitializeContainer(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            var conf = InitializeConfiguration();
            containerBuilder.RegisterInstance(conf).As<MasterConfiguration>();
            containerBuilder.RegisterInstance(conf).As<WebApiConfiguration>();
            containerBuilder.RegisterType<MasterServer>().PropertiesAutowired();
            containerBuilder.RegisterType<TokenController>().PropertiesAutowired();
            containerBuilder.Populate(services);
            return containerBuilder;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            PrintHeader();
            var container = InitializeContainer(services).Build();
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Info { Title = "NosCore Master API", Version = "v1" }));
            var keyByteArray = Encoding.ASCII.GetBytes(EncryptionHelper.Sha512(container.Resolve<MasterConfiguration>().Password));
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            services.AddAuthentication(config => config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(cfg =>
            {
                cfg.RequireHttpsMetadata = false;
                cfg.SaveToken = true;
                cfg.TokenValidationParameters = new TokenValidationParameters()
                {
                    IssuerSigningKey = signinKey,
                    ValidAudience = "Audience",
                    ValidIssuer = "Issuer",
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                };
            });

            services.AddMvc(o =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                o.Filters.Add(new AuthorizeFilter(policy));
            }).AddApplicationPart(typeof(TokenController).GetTypeInfo().Assembly).AddControllersAsServices();
            container = InitializeContainer(services).Build();
            Task.Run(() => container.Resolve<MasterServer>().Run());
            return new AutofacServiceProvider(container);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NosCore Master API"));
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
