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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Core.Encryption;
using NosCore.Dao.Interfaces;
using NosCore.Dao;
using NosCore.Data.Dto;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NosCore.Core.Configuration;
using NosCore.Database;
using AutofacSerilogIntegration;
using NosCore.Shared.I18N;
using NosCore.GameObject.Services.AuthService;

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