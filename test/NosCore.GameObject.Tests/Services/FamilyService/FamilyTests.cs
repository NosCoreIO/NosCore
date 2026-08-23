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
            characterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .ReturnsAsync((Expression<Func<CharacterDto, bool>> p) => characters.FirstOrDefault(p.Compile())!);

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
        public async Task ACharacterWithNoFamilyGetsNothingAsync()
        {
            Assert.IsNull(await Nemesis().GetFamilyAsync(999));
        }

        [TestMethod]
        public async Task TheFamilyComesBackWithEveryMemberAsync()
        {
            var family = await Nemesis().GetFamilyAsync(MemberId);

            Assert.IsNotNull(family);
            Assert.AreEqual("-Nemesis-", family.Name);
            Assert.AreEqual(2, family.Members.Count);
        }

        [TestMethod]
        public async Task EachMemberIsReadAtTheirOwnRankAsync()
        {
            // The window hands out permissions off this value, and reading somebody else's row
            // would grant them without throwing.
            var family = await Nemesis().GetFamilyAsync(MemberId);

            Assert.AreEqual(FamilyAuthority.Member, family!.AuthorityOf(MemberId));
            Assert.AreEqual(FamilyAuthority.Head, family.AuthorityOf(HeadId));
        }

        [TestMethod]
        public async Task ARankThatIsNotInTheListReadsAsMemberAsync()
        {
            var family = await Nemesis().GetFamilyAsync(MemberId);

            Assert.AreEqual(FamilyAuthority.Member, family!.AuthorityOf(999));
        }

        [TestMethod]
        public async Task TheHeadsNameIsFetchedForTheWindowAsync()
        {
            var family = await Nemesis().GetFamilyAsync(MemberId);

            Assert.AreEqual("Yzigor", family!.HeadCharacterName);
        }

        [TestMethod]
        public async Task AMembershipPointingAtAFamilyThatIsGoneReadsAsNoFamilyAsync()
        {
            // Rather than a family packet naming something that cannot be opened.
            var service = Build([],
                [new FamilyCharacterDto { FamilyCharacterId = 1, FamilyId = FamilyId, CharacterId = MemberId }],
                [new CharacterDto { CharacterId = MemberId, Name = "Uppermost" }]);

            Assert.IsNull(await service.GetFamilyAsync(MemberId));
        }

        [TestMethod]
        public void TheTwoAuthorityEnumsAgreeValueForValue()
        {
            // One enum lives in the database layer and one in the packet library, and the tag
            // and the window are built from different ones. If they ever diverge every player's
            // rank silently shifts by one.
            foreach (var authority in Enum.GetValues<FamilyAuthority>())
            {
                Assert.AreEqual(authority.ToString(),
                    ((PacketFamilyAuthority)authority).ToString());
            }
        }
    }
}
