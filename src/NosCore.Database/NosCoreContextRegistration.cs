//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NosCore.Database
{
    public static class NosCoreContextRegistration
    {
        public static DbContextOptionsBuilder UseNosCoreDatabase(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseNpgsql(connectionString, options => options.UseNodaTime());

        public static IServiceCollection AddNosCoreContext(this IServiceCollection services, string connectionString) =>
            services.AddDbContext<NosCoreContext>(builder => builder.UseNosCoreDatabase(connectionString));
    }
}
