//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class OpenFamilyWarehouseHistoryHandlerTests
    {
        private const long FamilyId = 7;
        private TestWorld World;
        private ClientSession _session;
        private Mock<IFamilyLogService> _familyLogService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            World.UseReal<IFamilyWarehouseService, FamilyWarehouseService>();
            _familyLogService = World.Freeze<Mock<IFamilyLogService>>();
            _familyLogService.Setup(s => s.GetWarehouseHistory(FamilyId)).Returns(new List<FamilyLogDto>
            {
                new()
                {
                    FamilyId = FamilyId,
                    FamilyLogType = FamilyLogType.WareHouseRemoved,
                    FirstArgument = "Bud",
                    SecondArgument = "1012",
                    ThirdArgument = "3",
                    Timestamp = World.Clock.GetCurrentInstant() - Duration.FromHours(2)
                }
            });
        }

        [TestMethod]
        public async Task AMemberAllowedToReadTheHistorySeesTheWarehouseEntries()
        {
            await new Spec("A member whose rank may read the history gets the warehouse history window")
                .Given(TheCharacterIsA_InAFamilyThatLetsMembersReadTheHistory_, FamilyAuthority.Member, true)
                .WhenAsync(OpeningTheWarehouseHistory)
                .Then(TheWindowShouldList_Taking_Of_Hours_Ago, "Bud", (short)1012, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMemberNotAllowedToReadTheHistoryIsTurnedAway()
        {
            await new Spec("A member whose rank may not read the history is told they are not authorised")
                .Given(TheCharacterIsA_InAFamilyThatLetsMembersReadTheHistory_, FamilyAuthority.Member, false)
                .WhenAsync(OpeningTheWarehouseHistory)
                .Then(TheyShouldBeToldTheyAreNotAuthorised)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheHeadAlwaysSeesTheHistory()
        {
            await new Spec("The head reads the history whatever the members may do")
                .Given(TheCharacterIsA_InAFamilyThatLetsMembersReadTheHistory_, FamilyAuthority.Head, false)
                .WhenAsync(OpeningTheWarehouseHistory)
                .Then(TheWindowShouldList_Taking_Of_Hours_Ago, "Bud", (short)1012, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SomeoneWithoutAFamilyIsTurnedAway()
        {
            await new Spec("A character outside any family is told they are not authorised")
                .WhenAsync(OpeningTheWarehouseHistory)
                .Then(TheyShouldBeToldTheyAreNotAuthorised)
                .ExecuteAsync();
        }

        private void TheCharacterIsA_InAFamilyThatLetsMembersReadTheHistory_(FamilyAuthority authority, bool membersMayRead) =>
            _session.Character.Family = new Family
            {
                FamilyId = FamilyId,
                Name = "Testers",
                MemberCanGetHistory = membersMayRead,
                Members = new List<FamilyCharacterDto>
                {
                    new() { FamilyId = FamilyId, CharacterId = _session.Character.CharacterId, Authority = authority }
                }
            };

        private Task OpeningTheWarehouseHistory() =>
            World.Create<OpenFamilyWarehouseHistoryHandler>().HandleAsync(_session, null,
                new NrunPacket { Runner = NrunRunnerType.OpenFamilyWarehouseHistory });

        private void TheWindowShouldList_Taking_Of_Hours_Ago(string name, short vnum, int hours)
        {
            var entry = _session.LastPackets.OfType<FslogStcPacket>().Single().Entries?.Single();
            Assert.IsNotNull(entry);
            Assert.AreEqual(FamilyWarehouseLogType.Withdrawal, entry.Type);
            Assert.AreEqual(name, entry.CharacterName);
            Assert.AreEqual(vnum, entry.ItemVNum);
            Assert.AreEqual(3, entry.Amount);
            Assert.AreEqual(hours, entry.HoursAgo);
        }

        private void TheyShouldBeToldTheyAreNotAuthorised()
        {
            Assert.AreEqual(Game18NConstString.NotAuthorized, _session.LastPackets.OfType<InfoiPacket>().Single().Message);
            Assert.IsFalse(_session.LastPackets.OfType<FslogStcPacket>().Any());
        }
    }
}
