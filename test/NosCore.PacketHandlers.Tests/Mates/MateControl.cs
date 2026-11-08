//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Mates
{
    internal static class MateControl
    {
        public static MateControlService Service(IBattleService? battleService = null,
            INpcCombatCatalog? catalog = null, List<SkillDto>? skills = null) =>
            new(TestHelpers.Instance.DistanceCalculator,
                battleService ?? new Mock<IBattleService>().Object,
                catalog ?? new Mock<INpcCombatCatalog>().Object,
                skills ?? new List<SkillDto>(),
                TestHelpers.Instance.Clock);
    }
}
