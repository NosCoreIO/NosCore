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
using NosCore.Packets.ServerPackets.Families;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Family = NosCore.GameObject.Services.FamilyService.Family;
using GenderType = NosCore.Packets.Enumerations.GenderType;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyPacketTests
    {
        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
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

            var packet = _session.Character.GenerateGidx(World.GameLanguageLocalizer,
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

            var packet = _session.Character.GenerateGidx(World.GameLanguageLocalizer,
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

            var packet = _session.Character.GenerateGInfo(new FamilyExperienceService()).NotNull();

            Assert.AreEqual("-Nemesis-", packet.FamilyName);
            Assert.AreEqual("Yzigor", packet.CharacterName);
            Assert.AreEqual(GenderType.Male, packet.FamilyHeadGenderType);
            Assert.AreEqual(7, packet.FamilyLevel);
            Assert.AreEqual(130000, packet.FamilyXp);
            Assert.AreEqual(70, packet.MembersCapacity);
            Assert.IsTrue(packet.FamilyManagerCanInvit);
            Assert.AreEqual("coin afk go rush", packet.FamilyMessage);
        }

        [TestMethod]
        public void NoFamilyMeansNoWindow()
        {
            _session.Character.Family = null;

            Assert.IsNull(_session.Character.GenerateGInfo(new FamilyExperienceService()));
        }

        [TestMethod]
        public void NoFamilyLeavesEveryFamilyFieldEmpty()
        {
            // gidx 1 741328 -1 - 0
            _session.Character.Family = null;

            PacketAssert.AreEqual(new GidxPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                FamilyId = null,
                Unknown = null,
                FamilyName = null,
                FamilyLevel = 0,
                FamilyIcons = [],
            }, _session.Character.GenerateGidx(World.GameLanguageLocalizer, RegionType.EN));
        }

        [TestMethod]
        public void EveryRankLabelIsASingleWord()
        {
            foreach (var authority in Enum.GetValues<FamilyAuthority>())
            {
                _session.Character.Family = Nemesis(_session.Character.CharacterId, authority);
                foreach (var language in Enum.GetValues<RegionType>())
                {
                    var tag = _session.Character.GenerateFamilyTag(World.GameLanguageLocalizer,
                        language).NotNull();
                    Assert.IsFalse(tag.Contains(' '), $"{authority} in {language} splits the tag: {tag}");
                }
            }
        }

        [TestMethod]
        public void AFamilyGoesOutWithItsIdNameAndLevel()
        {
            // gidx 1 521919 5052.-1 -Nemesis-(...) 7 0|0|0
            _session.Character.Family = Nemesis(_session.Character.CharacterId);

            PacketAssert.AreEqual(new GidxPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                FamilyId = 5052,
                Unknown = -1,
                FamilyName = _session.Character.GenerateFamilyTag(World.GameLanguageLocalizer, RegionType.EN),
                FamilyLevel = 7,
                FamilyIcons = [false, false, false],
            }, _session.Character.GenerateGidx(World.GameLanguageLocalizer, RegionType.EN));
        }

        [TestMethod]
        public void TheRankIsReadInTheLanguageOfWhoeverIsLooking()
        {
            _session.Character.Family = Nemesis(_session.Character.CharacterId, FamilyAuthority.Manager);

            var english = _session.Character
                .GenerateGidx(World.GameLanguageLocalizer, Shared.Enumerations.RegionType.EN)
                .FamilyName;
            var french = _session.Character
                .GenerateGidx(World.GameLanguageLocalizer, Shared.Enumerations.RegionType.FR)
                .FamilyName;

            Assert.AreNotEqual(english, french);
        }
    }
}
