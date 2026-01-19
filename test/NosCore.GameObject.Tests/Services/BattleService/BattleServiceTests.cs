//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BattleServiceTests
    {
        private IBattleService Service = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Service = new GameObject.Services.BattleService.BattleService();
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ServiceImplementsInterface()
        {
            await new Spec("Service implements interface")
                .Then(ServiceShouldImplementInterface)
                .ExecuteAsync();
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }

        private void ServiceShouldImplementInterface()
        {
            Assert.IsInstanceOfType(Service, typeof(IBattleService));
        }
    }
}
