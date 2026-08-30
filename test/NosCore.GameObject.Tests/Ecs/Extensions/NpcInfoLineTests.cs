//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Packets;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class NpcInfoLineTests
    {
        private static readonly Serializer Wire = new(typeof(IPacket).Assembly.GetTypes()
            .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract)
            .ToList());

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
        public void TheLineEndsWithThePortraitAndTheName()
        {
            var line = Wire.Serialize(new[] { (IPacket)Cuby().GenerateNpcInfo(RegionType.EN) }).TrimEnd();

            Assert.IsTrue(line.EndsWith("-1 Mother^Cuby"), line);
            Assert.AreEqual(26, line.Split(' ').Length - 1, line);
        }

        [TestMethod]
        public void TheNameIsTheReadersLanguage()
        {
            var line = Wire.Serialize(new[] { (IPacket)Cuby().GenerateNpcInfo(RegionType.FR) }).TrimEnd();

            Assert.IsTrue(line.EndsWith("-1 Cuby^Mere"), line);
        }
    }
}
