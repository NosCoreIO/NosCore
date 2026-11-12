//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Services.FamilyService;
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
    public class FamilyAuthorityTests
    {
        private const long Head = 1;
        private const long Assistant = 2;
        private const long Manager = 3;
        private const long Member = 4;
        private const long SecondMember = 5;

        private Family _family;
        private FamilyServiceImpl _service;
        private Mock<IDao<FamilyCharacterDto, long>> _memberships;

        [TestInitialize]
        public void Setup()
        {
            _family = new Family
            {
                FamilyId = 9,
                Name = "Testers",
                Members = new List<FamilyCharacterDto>
                {
                    new() { CharacterId = Head, Authority = FamilyAuthority.Head },
                    new() { CharacterId = Assistant, Authority = FamilyAuthority.Assistant },
                    new() { CharacterId = Manager, Authority = FamilyAuthority.Manager },
                    new() { CharacterId = Member, Authority = FamilyAuthority.Member },
                    new() { CharacterId = SecondMember, Authority = FamilyAuthority.Member }
                }
            };
            var rows = _family.Members
                .Select(s => new FamilyCharacterDto { CharacterId = s.CharacterId, Authority = s.Authority })
                .ToList();
            _memberships = new Mock<IDao<FamilyCharacterDto, long>>();
            _memberships.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<FamilyCharacterDto, bool>>>()))
                .ReturnsAsync((Expression<Func<FamilyCharacterDto, bool>> predicate) => rows.FirstOrDefault(predicate.Compile()));
            _memberships.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<FamilyCharacterDto>()))
                .ReturnsAsync((FamilyCharacterDto dto) => dto);
            _service = new FamilyServiceImpl(new Mock<IDao<FamilyDto, long>>().Object, _memberships.Object,
                new Mock<IDao<FamilyLogDto, long>>().Object, new Mock<IDao<CharacterDto, long>>().Object);
        }

        private FamilyAuthority AuthorityOf(long id) => _family.AuthorityOf(id);

        [TestMethod]
        public async Task TheHeadAppointsADeputy()
        {
            var result = await _service.ChangeAuthorityAsync(_family, Head, Member, FamilyAuthority.Assistant);

            Assert.AreEqual(FamilyAuthorityChange.Changed, result);
            Assert.AreEqual(FamilyAuthority.Assistant, AuthorityOf(Member));
        }

        [TestMethod]
        public async Task AFamilyHasAtMostTwoDeputies()
        {
            await _service.ChangeAuthorityAsync(_family, Head, Member, FamilyAuthority.Assistant);

            var result = await _service.ChangeAuthorityAsync(_family, Head, SecondMember, FamilyAuthority.Assistant);

            Assert.AreEqual(FamilyAuthorityChange.TooManyAssistants, result);
            Assert.AreEqual(FamilyAuthority.Member, AuthorityOf(SecondMember));
        }

        [TestMethod]
        public async Task OnlyTheHeadAppointsDeputies()
        {
            var result = await _service.ChangeAuthorityAsync(_family, Assistant, Member, FamilyAuthority.Assistant);

            Assert.AreEqual(FamilyAuthorityChange.NotAuthorized, result);
        }

        [TestMethod]
        public async Task TheHeadPassesTheFamilyToADeputyAndBecomesOne()
        {
            var result = await _service.ChangeAuthorityAsync(_family, Head, Assistant, FamilyAuthority.Head);

            Assert.AreEqual(FamilyAuthorityChange.Changed, result);
            Assert.AreEqual(FamilyAuthority.Head, AuthorityOf(Assistant));
            Assert.AreEqual(FamilyAuthority.Assistant, AuthorityOf(Head));
        }

        [TestMethod]
        public async Task OnlyADeputyCanBecomeHead()
        {
            var result = await _service.ChangeAuthorityAsync(_family, Head, Member, FamilyAuthority.Head);

            Assert.AreEqual(FamilyAuthorityChange.OnlyAssistantCanBecomeHead, result);
            Assert.AreEqual(FamilyAuthority.Head, AuthorityOf(Head));
        }

        [TestMethod]
        public async Task ADeputyAppointsAndDemotesKeepers()
        {
            Assert.AreEqual(FamilyAuthorityChange.Changed,
                await _service.ChangeAuthorityAsync(_family, Assistant, Member, FamilyAuthority.Manager));
            Assert.AreEqual(FamilyAuthorityChange.Changed,
                await _service.ChangeAuthorityAsync(_family, Assistant, Manager, FamilyAuthority.Member));
            Assert.AreEqual(FamilyAuthority.Manager, AuthorityOf(Member));
            Assert.AreEqual(FamilyAuthority.Member, AuthorityOf(Manager));
        }

        [TestMethod]
        public async Task ADeputyCannotDemoteTheHeadOrAnotherDeputy()
        {
            Assert.AreEqual(FamilyAuthorityChange.NotAuthorized,
                await _service.ChangeAuthorityAsync(_family, Assistant, Head, FamilyAuthority.Member));
            await _service.ChangeAuthorityAsync(_family, Head, Member, FamilyAuthority.Assistant);
            Assert.AreEqual(FamilyAuthorityChange.NotAuthorized,
                await _service.ChangeAuthorityAsync(_family, Assistant, Member, FamilyAuthority.Manager));
        }

        [TestMethod]
        public async Task KeepersAndMembersChangeNothing()
        {
            Assert.AreEqual(FamilyAuthorityChange.NotAuthorized,
                await _service.ChangeAuthorityAsync(_family, Manager, Member, FamilyAuthority.Manager));
            Assert.AreEqual(FamilyAuthorityChange.NotAuthorized,
                await _service.ChangeAuthorityAsync(_family, Member, SecondMember, FamilyAuthority.Manager));
            _memberships.Verify(s => s.TryInsertOrUpdateAsync(It.IsAny<FamilyCharacterDto>()), Times.Never);
        }

        [TestMethod]
        public async Task SomeoneOutsideTheFamilyIsNotFound()
        {
            var result = await _service.ChangeAuthorityAsync(_family, Head, 99, FamilyAuthority.Manager);

            Assert.AreEqual(FamilyAuthorityChange.NotInFamily, result);
        }
    }
}
