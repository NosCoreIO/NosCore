//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class NpcInfoLineTests
    {
        private static NpcMonsterDto Cuby()
        {
            var name = new I18NString
            {
                [RegionType.EN] = "Mother Cuby",
                [RegionType.FR] = "Cuby Mere"
            };

            return new NpcMonsterDto
            {
                NpcMonsterVNum = 303,
                Level = 35,
                MaxHp = 1360,
                MaxMp = 630,
                Name = name
            };
        }

        [TestMethod]
        public void TheInfoCarriesTheStatsThePortraitAndTheName()
        {
            PacketAssert.AreEqual(new EInfoNpcMonsterPacket
            {
                SubType = 10,
                NpcMonsterVNum = 303,
                Level = 35,
                MaxHp = 1360,
                MaxMp = 630,
                Unknown = -1,
                Name = "Mother Cuby",
            }, Cuby().GenerateNpcInfo(RegionType.EN));
        }

        [TestMethod]
        public void TheNameIsTheReadersLanguage()
        {
            Assert.AreEqual("Cuby Mere", Cuby().GenerateNpcInfo(RegionType.FR).Name);
        }
    }
}
