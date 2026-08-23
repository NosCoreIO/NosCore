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
using NosCore.Tests.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Family = NosCore.GameObject.Services.FamilyService.Family;
using FamilyCharacter = NosCore.GameObject.Services.FamilyService.FamilyCharacter;
using GenderType = NosCore.Packets.Enumerations.GenderType;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    // The two packets that carry a family, built off a real character the way the server builds
    // them. Every value asserted here comes from a captured line rather than from what the code
    // happens to produce.
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

        private static FamilyCharacter Nemesis(FamilyAuthority authority = FamilyAuthority.Member)
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
            var membership = new FamilyCharacter
            {
                FamilyCharacterId = 1,
                FamilyId = family.FamilyId,
                Authority = authority,
                Family = family
            };
            family.Members = new List<FamilyCharacter> { membership };
            return membership;
        }

        [TestMethod]
        public void TheTagCarriesTheFamilyIdAndTheRank()
        {
            // gidx 1 521919 5052 -Nemesis-(Member) 7
            _session.Character.FamilyCharacter = Nemesis();

            var packet = _session.Character.GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer,
                _session.Character.AccountLanguage);

            Assert.AreEqual(5052, packet.FamilyId);
            Assert.AreEqual(7, packet.FamilyLevel);
            StringAssert.StartsWith(packet.FamilyName, "-Nemesis-(");
        }

        [TestMethod]
        public void ACharacterWithNoFamilyIsStillToldSo()
        {
            // gidx 1 741328 -1 - 0. Silence would leave the client showing whichever tag it was
            // given last, and a null id is what the serializer writes as -1.
            _session.Character.FamilyCharacter = null;

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
            _session.Character.FamilyCharacter = Nemesis();

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
            _session.Character.FamilyCharacter = null;

            Assert.IsNull(_session.Character.GenerateGInfo(new FamilyExperienceService()));
        }

        [TestMethod]
        public void TheRankIsReadInTheLanguageOfWhoeverIsLooking()
        {
            // Two players standing next to each other see the same family in their own words,
            // so the tag cannot be built once from the owner's account language.
            _session.Character.FamilyCharacter = Nemesis(FamilyAuthority.Manager);

            var english = _session.Character
                .GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer, Shared.Enumerations.RegionType.EN)
                .FamilyName;
            var french = _session.Character
                .GenerateGidx(TestHelpers.Instance.GameLanguageLocalizer, Shared.Enumerations.RegionType.FR)
                .FamilyName;

            // The words themselves live in the resources — the capture shows Gardien for a
            // Manager on a French session. What this guards is that the tag is built per
            // reader at all, rather than once from the owner's account language.
            Assert.AreNotEqual(english, french);
        }
    }
}
