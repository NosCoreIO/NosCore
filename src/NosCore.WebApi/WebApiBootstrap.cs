//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.Services.AuthService;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.WebApi
{
    public class WebApiBootstrap
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddRazorPages();
            var loginConfiguration = new ApiConfiguration();
            var conf = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "api.yml" });
            conf.Bind(loginConfiguration);
            builder.Services.AddDbContext<NosCoreContext>(
                conf => conf.UseNpgsql(loginConfiguration.Database.ConnectionString, options => { options.UseNodaTime(); }));
            builder.Services.AddOptions<LoginConfiguration>().Bind(conf).ValidateDataAnnotations();
            builder.Services.AddOptions<ServerConfiguration>().Bind(conf).ValidateDataAnnotations();
            builder.Services.AddOptions<WebApiConfiguration>().Bind(conf.GetSection(nameof(ApiConfiguration.MasterCommunication))).ValidateDataAnnotations();
            builder.Services.AddI18NLogs();

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(
                containerBuilder =>
                {
                    containerBuilder.RegisterType<AuthHub>().AsImplementedInterfaces();
                    containerBuilder.RegisterType<AuthCodeService>().As<IAuthCodeService>().SingleInstance();
                    containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
                    containerBuilder.RegisterLogger();
                    containerBuilder.RegisterType<Dao<Account, AccountDto, long>>().As<IDao<AccountDto, long>>().SingleInstance();

                    containerBuilder.Register<IHasher>(o => o.Resolve<IOptions<WebApiConfiguration>>().Value.HashingType switch
                    {
                        HashingType.BCrypt => new BcryptHasher(),
                        HashingType.Pbkdf2 => new Pbkdf2Hasher(),
                        _ => new Sha512Hasher()
                    });
                });

            var app = builder.Build();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();
            app.Run();
        }
    }
}
