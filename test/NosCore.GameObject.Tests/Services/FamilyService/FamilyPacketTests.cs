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
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Family = NosCore.GameObject.Services.FamilyService.Family;
using Game18NConstString = NosCore.Packets.Enumerations.Game18NConstString;
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
        public void TheTagCarriesTheFamilyIdTheRankAndTheBareName()
        {
            // gidx 1 14819755 7752.917 Testers 11 0|0|0
            _session.Character.Family = Nemesis(_session.Character.CharacterId);

            var packet = _session.Character.GenerateGidx();

            Assert.AreEqual(5052, packet.FamilyId);
            Assert.AreEqual(Game18NConstString.Member, packet.FamilyRank);
            Assert.AreEqual("-Nemesis-", packet.FamilyName);
            Assert.AreEqual(7, packet.FamilyLevel);
        }

        [TestMethod]
        [DataRow(FamilyAuthority.Head, Game18NConstString.Familyhead)]
        [DataRow(FamilyAuthority.Assistant, Game18NConstString.Familydeputy)]
        [DataRow(FamilyAuthority.Manager, Game18NConstString.Familykeeper)]
        [DataRow(FamilyAuthority.Member, Game18NConstString.Member)]
        public void EachAuthorityIsShownWithTheClientsOwnRankText(FamilyAuthority authority, Game18NConstString rank)
        {
            _session.Character.Family = Nemesis(_session.Character.CharacterId, authority);

            Assert.AreEqual(rank, _session.Character.GenerateGidx().FamilyRank);
            Assert.AreEqual(rank, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull().FamilySubPacket.NotNull().FamilyRank);
            Assert.AreEqual(rank, _session.Character.GenerateCInfo(World.GameLanguageLocalizer).FamilyRank);
        }

        [TestMethod]
        public void OtherPlayersSeeTheFamilyInTheMapEntry()
        {
            // in 1 Alice - 6805743 ... 1618.917 Testers 27 ...
            _session.Character.Family = Nemesis(_session.Character.CharacterId);

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();

            PacketAssert.AreEqual(new FamilySubPacket { FamilyId = 5052, FamilyRank = Game18NConstString.Member },
                sub.FamilySubPacket.NotNull());
            Assert.AreEqual("-Nemesis-", sub.FamilyName);
        }

        [TestMethod]
        public void AMapEntryWithoutAFamilyHasNoFamilyField()
        {
            // in 1 Bob - 12738248 ... -1 - 23 ...
            _session.Character.Family = null;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();

            Assert.IsNull(sub.FamilySubPacket, "the serializer writes an absent family as -1");
            Assert.IsNull(sub.FamilyName, "the serializer writes a null string as -");
        }

        [TestMethod]
        public void TheOwnCharacterInfoCarriesTheFamily()
        {
            _session.Character.Family = Nemesis(_session.Character.CharacterId, FamilyAuthority.Head);

            var packet = _session.Character.GenerateCInfo(World.GameLanguageLocalizer);

            Assert.AreEqual((5052, Game18NConstString.Familyhead, "-Nemesis-"),
                (packet.FamilyId, packet.FamilyRank, packet.FamilyName));
        }

        [TestMethod]
        public void TheOwnCharacterInfoWithoutAFamilyHasNoRank()
        {
            // c_info Tester - -1 -1 - 14293841 ...
            _session.Character.Family = null;

            var packet = _session.Character.GenerateCInfo(World.GameLanguageLocalizer);

            Assert.AreEqual((-1, (Game18NConstString?)null, (string?)null),
                (packet.FamilyId, packet.FamilyRank, packet.FamilyName));
        }

        [TestMethod]
        public void ACharacterWithNoFamilyIsStillToldSo()
        {
            // Silence would leave the last tag on screen.
            _session.Character.Family = null;

            var packet = _session.Character.GenerateGidx();

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
            _session.Character.Family = null;

            PacketAssert.AreEqual(new GidxPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                FamilyId = null,
                FamilyRank = null,
                FamilyName = null,
                FamilyLevel = 0,
                FamilyIcons = [0, 0, 0],
            }, _session.Character.GenerateGidx());
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
            _session.Character.Family = Nemesis(_session.Character.CharacterId);

            PacketAssert.AreEqual(new GidxPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                FamilyId = 5052,
                FamilyRank = Game18NConstString.Member,
                FamilyName = "-Nemesis-",
                FamilyLevel = 7,
                FamilyIcons = [0, 0, 0],
            }, _session.Character.GenerateGidx());
        }

        [TestMethod]
        public void TheProfileCardReadsTheRankInTheLanguageOfWhoeverIsLooking()
        {
            _session.Character.Family = Nemesis(_session.Character.CharacterId, FamilyAuthority.Manager);

            var english = _session.Character
                .GenerateReqInfo(World.GameLanguageLocalizer, Shared.Enumerations.RegionType.EN)
                .FamilyName;
            var french = _session.Character
                .GenerateReqInfo(World.GameLanguageLocalizer, Shared.Enumerations.RegionType.FR)
                .FamilyName;

            Assert.AreNotEqual(english, french);
        }
    }
}
