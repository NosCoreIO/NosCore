using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.PacketHandlers.Bazaar;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CSkillPacketHandlerTest
    {
        private CSkillPacketHandler _cskillPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            _cskillPacketHandler = new CSkillPacketHandler();
        }
    }
}
