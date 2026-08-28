//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets;
using NosCore.Shared.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Family = NosCore.GameObject.Services.FamilyService.Family;
using GenderType = NosCore.Packets.Enumerations.GenderType;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyPacketTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        private static Family Nemesis(long characterId, FamilyAuthority authority = FamilyAuthority.Member)
        {
            var family = new Family
            {
                FamilyId = 5052,
                Name = "-Nemesis-",
                FamilyLevel = 7,
                FamilyExperience = 130000,
                MaxSize = 70,
                FamilyHeadGender = GenderType.Male,
                ManagerCanInvite = true,
                ManagerCanNotice = true,
                ManagerCanShout = true,
                ManagerCanGetHistory = true,
                MemberCanGetHistory = true,
                FamilyMessage = "coin afk go rush",
                HeadCharacterName = "Yzigor"
            };
            family.Members = new List<FamilyCharacterDto>
            {
                new()
                {
                    FamilyCharacterId = 1,
                    FamilyId = family.FamilyId,
                    CharacterId = characterId,
                    Authority = authority
                }
            };
            return family;
        }

        [TestMethod]
        public void TheTagCarriesTheFamilyIdAndTheRank()
        {
            // gidx 1 521919 5052 -Nemesis-(Member) 7
            _session.Character.Family = Nemesis(_session.Character.CharacterId);

            var packet = _session.Character.GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer,
                _session.Character.AccountLanguage);

            Assert.AreEqual(5052, packet.FamilyId);
            Assert.AreEqual(7, packet.FamilyLevel);
            StringAssert.StartsWith(packet.FamilyName, "-Nemesis-(");
        }

        [TestMethod]
        public void ACharacterWithNoFamilyIsStillToldSo()
        {
            // gidx 1 741328 -1 - 0 - silence would leave the last tag on screen.
            _session.Character.Family = null;

            var packet = _session.Character.GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer,
                _session.Character.AccountLanguage);

            Assert.IsNull(packet.FamilyId);
            Assert.IsNull(packet.FamilyName, "the serializer writes a null string as -");
            Assert.AreEqual(0, packet.FamilyLevel);
        }

        [TestMethod]
        public void TheWindowMatchesTheCapturedLine()
        {
            // ginfo -Nemesis- Yzigor 0 7 130000 640000 68 70 3 1 1 1 1 2 1 2 coin^afk^go^rush
            _session.Character.Family = Nemesis(_session.Character.CharacterId);

            var packet = _session.Character.GenerateGInfo(new FamilyExperienceService())!;

            Assert.AreEqual("-Nemesis-", packet.FamilyName);
            Assert.AreEqual("Yzigor", packet.CharacterName);
            Assert.AreEqual(GenderType.Male, packet.FamilyHeadGenderType);
            Assert.AreEqual(7, packet.FamilyLevel);
            Assert.AreEqual(130000, packet.FamilyXp);
            Assert.AreEqual(70, packet.MembersCapacity);
            Assert.IsTrue(packet.FamilyManagerCanInvit);
            Assert.AreEqual("coin afk go rush", packet.FamilyMessage,
                "the caret substitution is the serializer's job, not ours");
        }

        [TestMethod]
        public void NoFamilyMeansNoWindow()
        {
            _session.Character.Family = null;

            Assert.IsNull(_session.Character.GenerateGInfo(new FamilyExperienceService()));
        }

        [TestMethod]
        public void TheTagGoesOutOnTheWireExactlyAsTheCaptureHasIt()
        {
            var serializer = BuildSerializer();

            _session.Character.Family = null;
            var line = serializer.Serialize(new[]
                { (IPacket)_session.Character.GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer,
                    RegionType.EN) }).TrimEnd('\uFFFF', '\n', ' ');

            StringAssert.StartsWith(line, "gidx 1 ");
            StringAssert.EndsWith(line, " -1 - 0",
                "no family has to reach the client as -1 and -, not as empty fields");
        }

        [TestMethod]
        public void AFamilyGoesOutWithItsIdNameAndLevel()
        {
            // gidx 1 521919 5052 -Nemesis-(...) 7
            var serializer = BuildSerializer();

            _session.Character.Family = Nemesis(_session.Character.CharacterId);
            var line = serializer.Serialize(new[]
                { (IPacket)_session.Character.GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer,
                    RegionType.EN) }).TrimEnd('\uFFFF', '\n', ' ');

            var fields = line.Split(' ');
            Assert.AreEqual("gidx", fields[0]);
            Assert.AreEqual("1", fields[1]);
            Assert.AreEqual("5052", fields[3]);
            StringAssert.StartsWith(fields[4], "-Nemesis-(");
            Assert.AreEqual("7", fields[5]);
        }

        private static Serializer BuildSerializer() => new(typeof(IPacket).Assembly.GetTypes()
            .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract)
            .ToList());

        [TestMethod]
        public void TheRankIsReadInTheLanguageOfWhoeverIsLooking()
        {
            _session.Character.Family = Nemesis(_session.Character.CharacterId, FamilyAuthority.Manager);

            var english = _session.Character
                .GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer, Shared.Enumerations.RegionType.EN)
                .FamilyName;
            var french = _session.Character
                .GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer, Shared.Enumerations.RegionType.FR)
                .FamilyName;

            Assert.AreNotEqual(english, french);
        }
    }
}
