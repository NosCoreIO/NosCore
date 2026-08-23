//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Enumerations;
using GenderType = NosCore.Packets.Enumerations.GenderType;
using PacketFamilyAuthority = NosCore.Packets.Enumerations.FamilyAuthority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Family = NosCore.GameObject.Services.FamilyService.Family;
using FamilyServiceImpl = NosCore.GameObject.Services.FamilyService.FamilyService;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyTests
    {
        private const long FamilyId = 5052;
        private const long HeadId = 100;
        private const long MemberId = 626114;

        private static IGameLanguageLocalizer Localizer()
        {
            var localizer = new Mock<IGameLanguageLocalizer>();
            localizer.Setup(s => s[It.IsAny<LanguageKey>(), It.IsAny<RegionType>()])
                .Returns((LanguageKey key, RegionType _) => new LocalizedString(key.ToString(), key switch
                {
                    LanguageKey.FAMILY_AUTHORITY_HEAD => "Head",
                    LanguageKey.FAMILY_AUTHORITY_ASSISTANT => "Assistant",
                    LanguageKey.FAMILY_AUTHORITY_MANAGER => "Manager",
                    _ => "Member"
                }));
            return localizer.Object;
        }

        private static FamilyDto NemesisDto()
        {
            return new FamilyDto
            {
                FamilyId = FamilyId,
                Name = "-Nemesis-",
                FamilyLevel = 7,
                FamilyExperience = 130000,
                MaxSize = 70,
                FamilyHeadGender = GenderType.Male,
                ManagerCanInvite = true,
                ManagerCanNotice = true,
                ManagerCanShout = true,
                ManagerCanGetHistory = true,
                ManagerAuthorityType = NosCore.Packets.Enumerations.FamilyAuthorityType.ALL,
                MemberCanGetHistory = true,
                MemberAuthorityType = NosCore.Packets.Enumerations.FamilyAuthorityType.ALL,
                FamilyMessage = "coin afk go rush"
            };
        }

        private static FamilyServiceImpl Build(IEnumerable<FamilyDto> families,
            IEnumerable<FamilyCharacterDto> memberships, IEnumerable<CharacterDto> characters)
        {
            var familyDao = new Mock<IDao<FamilyDto, long>>();
            familyDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<FamilyDto, bool>>>()))
                .ReturnsAsync((Expression<Func<FamilyDto, bool>> p) => families.FirstOrDefault(p.Compile())!);

            var membershipDao = new Mock<IDao<FamilyCharacterDto, long>>();
            membershipDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<FamilyCharacterDto, bool>>>()))
                .ReturnsAsync((Expression<Func<FamilyCharacterDto, bool>> p) => memberships.FirstOrDefault(p.Compile())!);
            membershipDao.Setup(s => s.Where(It.IsAny<Expression<Func<FamilyCharacterDto, bool>>>()))
                .Returns((Expression<Func<FamilyCharacterDto, bool>> p) => memberships.Where(p.Compile()));

            var characterDao = new Mock<IDao<CharacterDto, long>>();
            characterDao.Setup(s => s.Where(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> p) => characters.Where(p.Compile()));

            return new FamilyServiceImpl(familyDao.Object, membershipDao.Object, characterDao.Object);
        }

        private static FamilyServiceImpl Nemesis()
        {
            return Build(
                [NemesisDto()],
                [
                    new FamilyCharacterDto { FamilyCharacterId = 1, FamilyId = FamilyId, CharacterId = HeadId, Authority = FamilyAuthority.Head },
                    new FamilyCharacterDto { FamilyCharacterId = 2, FamilyId = FamilyId, CharacterId = MemberId, Authority = FamilyAuthority.Member }
                ],
                [
                    new CharacterDto { CharacterId = HeadId, Name = "Yzigor" },
                    new CharacterDto { CharacterId = MemberId, Name = "Uppermost" }
                ]);
        }

        [TestMethod]
        public async Task ACharacterWithNoFamilyHasNoMembershipAsync()
        {
            Assert.IsNull(await Nemesis().GetMembershipAsync(999));
        }

        [TestMethod]
        public async Task TheMembershipComesBackWithItsFamilyAndEveryMemberAsync()
        {
            var membership = await Nemesis().GetMembershipAsync(MemberId);

            Assert.IsNotNull(membership);
            Assert.AreEqual("-Nemesis-", membership.Family.Name);
            Assert.AreEqual(2, membership.Family.Members.Count);
            Assert.AreEqual(FamilyAuthority.Member, membership.Authority);
        }

        [TestMethod]
        public async Task TheMembershipHandedBackIsTheSameObjectTheFamilyHoldsAsync()
        {
            var membership = await Nemesis().GetMembershipAsync(MemberId);

            Assert.AreSame(membership,
                membership!.Family.Members.Single(s => s.CharacterId == MemberId));
        }

        [TestMethod]
        public async Task TheHeadIsFoundByAuthorityAndCarriesItsNameAsync()
        {
            var membership = await Nemesis().GetMembershipAsync(MemberId);

            Assert.AreEqual("Yzigor", membership!.Family.Head!.CharacterName);
        }

        [TestMethod]
        public async Task AMembershipPointingAtAFamilyThatIsGoneReadsAsNoFamilyAsync()
        {
            var service = Build([],
                [new FamilyCharacterDto { FamilyCharacterId = 1, FamilyId = FamilyId, CharacterId = MemberId }],
                [new CharacterDto { CharacterId = MemberId, Name = "Uppermost" }]);

            Assert.IsNull(await service.GetMembershipAsync(MemberId));
        }

        [TestMethod]
        public async Task TheFamilyWindowMatchesTheCapturedLineAsync()
        {
            var membership = await Nemesis().GetMembershipAsync(MemberId);
            var packet = membership!.Family.GenerateGInfo(membership.Authority, 640000);

            Assert.AreEqual("-Nemesis-", packet.FamilyName);
            Assert.AreEqual("Yzigor", packet.CharacterName);
            Assert.AreEqual(GenderType.Male, packet.FamilyHeadGenderType);
            Assert.AreEqual(7, packet.FamilyLevel);
            Assert.AreEqual(130000, packet.FamilyXp);
            Assert.AreEqual(640000u, packet.MaxFamilyXp);
            Assert.AreEqual(70, packet.MembersCapacity);
            Assert.AreEqual(PacketFamilyAuthority.Member, packet.CharacterFamilyAuthority);
            Assert.IsTrue(packet.FamilyManagerCanInvit);
        }

        [TestMethod]
        public async Task TheFamilyMessageTravelsWithoutSpacesAsync()
        {
            var membership = await Nemesis().GetMembershipAsync(MemberId);

            Assert.AreEqual("coin^afk^go^rush",
                membership!.Family.GenerateGInfo(membership.Authority, 1).FamilyMessage);
        }

        [TestMethod]
        public async Task TheTagIsTheFamilyNameWithTheRankAfterItAsync()
        {
            var membership = await Nemesis().GetMembershipAsync(MemberId);

            Assert.AreEqual("-Nemesis-(Member)",
                membership!.Family.GenerateFamilyTag(membership.Authority, Localizer(), RegionType.EN));
            Assert.AreEqual("-Nemesis-(Head)",
                membership.Family.GenerateFamilyTag(FamilyAuthority.Head, Localizer(), RegionType.EN));
        }

        [TestMethod]
        public void TheTwoAuthorityEnumsAgreeValueForValue()
        {
            foreach (var authority in Enum.GetValues<FamilyAuthority>())
            {
                Assert.AreEqual(authority.ToString(),
                    ((PacketFamilyAuthority)authority).ToString());
            }
        }

        [TestMethod]
        public async Task TheTagOverTheHeadMatchesTheCapturedLineAsync()
        {
            // gidx 1 521919 5083 [NDM](Gardien) 3 — a single family id, which is what all 670
            // gidx lines in the capture carry, and what the packet models since 21.0.0.
            var membership = await Nemesis().GetMembershipAsync(MemberId);
            var packet = membership!.Family.GenerateGidx(521919, membership.Authority,
                Localizer(), RegionType.EN);

            Assert.AreEqual(VisualType.Player, packet.VisualType);
            Assert.AreEqual(521919, packet.VisualId);
            Assert.AreEqual(FamilyId, packet.FamilyId);
            Assert.AreEqual("-Nemesis-(Member)", packet.FamilyName);
            Assert.AreEqual(7, packet.FamilyLevel);
        }

        [TestMethod]
        public void ACharacterWithNoFamilyStillGetsATag()
        {
            // gidx 1 741328 -1 - 0. Saying nothing would leave the client showing whichever tag
            // it was told about last.
            var packet = Family.GenerateEmptyGidx(741328);

            Assert.IsNull(packet.FamilyId, "a null id is what the serializer writes as -1");
            Assert.AreEqual("-", packet.FamilyName);
            Assert.AreEqual(0, packet.FamilyLevel);
        }
    }
}
