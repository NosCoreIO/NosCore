//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests
{
    [TestClass]
    public class SetupAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext _)
        {
            TestHelpers.Instance.InitDatabase();
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IStaticDto>()
                .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
        }
    }
}
