//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    internal static class ManaShieldMock
    {
        public static IManaShieldService Shieldless()
        {
            var shield = new Mock<IManaShieldService>();
            shield.Setup(s => s.AbsorbAsync(It.IsAny<IAliveEntity>(), It.IsAny<int>()))
                .ReturnsAsync((IAliveEntity _, int damage) => damage);
            return shield.Object;
        }
    }
}
