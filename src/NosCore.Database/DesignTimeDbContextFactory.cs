//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NosCore.Shared.Configuration;

namespace NosCore.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NosCoreContext>
    {
        public NosCoreContext CreateDbContext(string[] args)
        {
            var databaseConfiguration = new SqlConnectionConfiguration();
            ConfiguratorBuilder.InitializeConfiguration(args, new[] { "database.yml" }).Bind(databaseConfiguration);
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(databaseConfiguration.ConnectionString, options => { options.UseNodaTime(); });
            return new NosCoreContext(optionsBuilder.Options);
        }
    }
}
