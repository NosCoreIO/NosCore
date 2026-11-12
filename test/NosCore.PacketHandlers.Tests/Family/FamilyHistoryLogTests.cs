//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.AutoFixture;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyDismissPacket = NosCore.Packets.ClientPackets.Commands.FamilyDismissPacket;
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyHistoryLogTests
    {
        private TestWorld World;
        private const long FamilyId = 42;
        private readonly List<FamilyLogDto> _logs = new();
        private ClientSession _head;
        private ClientSession _other;
        private Mock<IDao<FamilyLogDto, long>> _logDao;
        private Mock<IFamilyService> _familyService;
        private Mock<IPubSubHub> _pubSubHub;
        private Mock<ISessionRegistry> _sessionRegistry;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.UseReal<IFamilyManagementService, FamilyManagementService>();
            World.UseReal<IFamilyLogService, FamilyLogService>();
            _logDao = World.Freeze<Mock<IDao<FamilyLogDto, long>>>();
            _familyService = World.Freeze<Mock<IFamilyService>>();
            _pubSubHub = World.PubSubHub;
            _sessionRegistry = World.Freeze<Mock<ISessionRegistry>>();
            World.Inject(_sessionRegistry.Object);
            _logs.Clear();
            _head = await World.CreateSessionAsync();
            _other = await World.CreateSessionAsync();
            _head.Character.Family = FamilyOf((_head.Character.CharacterId, FamilyAuthority.Head));
            _other.Character.Family = null;

            _logDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<FamilyLogDto>()))
                .Callback<FamilyLogDto>(_logs.Add)
                .ReturnsAsync((FamilyLogDto log) => log);
            _logDao.Setup(s => s.Where(It.IsAny<Expression<Func<FamilyLogDto, bool>>>()))
                .Returns((Expression<Func<FamilyLogDto, bool>> p) => _logs.Where(p.Compile()));

            _familyService.Setup(s => s.UpdateAsync(It.IsAny<long>(), It.IsAny<FamilyChange?>(), It.IsAny<IReadOnlyList<FamilyMemberChange>>())).ReturnsAsync(true);
            _familyService.Setup(s => s.AddMemberAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(FamilyMemberAddition.Added);
            _familyService.Setup(s => s.RemoveMemberAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);
            var otherName = _other.Character.Name;
            _familyService.Setup(s => s.FindCharacterAsync(otherName))
                .ReturnsAsync(new CharacterDto { CharacterId = _other.Character.CharacterId, Name = _other.Character.Name });
            _familyService.Setup(s => s.GetMemberCharactersAsync(It.IsAny<GameObject.Services.FamilyService.Family>()))
                .ReturnsAsync(new List<CharacterDto> { new() { CharacterId = _other.Character.CharacterId, Name = _other.Character.Name } });
            _pubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).ReturnsAsync(true);
            _pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            var head = _head.Character;
            _sessionRegistry.Setup(s => s.TryGetCharacter(It.IsAny<Func<GameObject.Ecs.PlayerComponentBundle, bool>>(), out head))
                .Returns(true);
        }

        private static GameObject.Services.FamilyService.Family FamilyOf(params (long CharacterId, FamilyAuthority Authority)[] members) => new()
        {
            FamilyId = FamilyId,
            Name = "Testers",
            MaxSize = 50,
            Members = members.Select(m => new FamilyCharacterDto
            {
                FamilyId = FamilyId,
                CharacterId = m.CharacterId,
                Authority = m.Authority,
                DailyMessage = string.Empty
            }).ToList()
        };

        [TestMethod]
        public async Task JoiningAFamilyIsLogged()
        {
            await new Spec("Accepting an invite logs who invited the newcomer, then the newcomer")
                .Given(TheHeadInvitedTheOtherPlayer)
                .WhenAsync(TheOtherPlayerAnswers_, FamilyInviteAnswerType.Accept)
                .Then(TheHistoryShouldHoldOnly_With_, FamilyLogType.UserManaged, new[] { "{head}", "{other}" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefusingAnInviteIsNotLogged()
        {
            await new Spec("Refusing an invite leaves the history alone")
                .Given(TheHeadInvitedTheOtherPlayer)
                .WhenAsync(TheOtherPlayerAnswers_, (FamilyInviteAnswerType)0)
                .Then(TheHistoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingAFamilyIsLogged()
        {
            await new Spec("A member leaving logs their name")
                .Given(TheOtherPlayerIsA_, FamilyAuthority.Member)
                .WhenAsync(TheOtherPlayerLeaves)
                .Then(TheHistoryShouldHoldOnly_With_, FamilyLogType.FamilyManaged, new[] { "{other}" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheHeadCannotLeaveSoNothingIsLogged()
        {
            await new Spec("The head's refused leave leaves the history alone")
                .WhenAsync(TheHeadTriesToLeave)
                .Then(TheHistoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DismissingAMemberIsLogged()
        {
            await new Spec("Dismissing a member logs the dismissed member's name")
                .Given(TheOtherPlayerIsA_, FamilyAuthority.Member)
                .WhenAsync(TheHeadDismissesTheOtherPlayer)
                .Then(TheHistoryShouldHoldOnly_With_, FamilyLogType.FamilyManaged, new[] { "{other}" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedDismissIsNotLogged()
        {
            await new Spec("A plain member cannot dismiss, and nothing is logged")
                .Given(TheOtherPlayerIsA_, FamilyAuthority.Member)
                .WhenAsync(TheOtherPlayerDismissesTheHead)
                .Then(TheHistoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingARightIsLogged()
        {
            await new Spec("The head letting keepers read the history logs who, which rank, which right and its value")
                .WhenAsync(TheHeadLetsKeepersReadTheHistory)
                .Then(TheHistoryShouldHoldOnly_With_, FamilyLogType.RightChanged, new[] { "{head}", "2", "4", "1" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheHeadsRightChangesAreLogged()
        {
            await new Spec("A member's right change is ignored and not logged")
                .Given(TheOtherPlayerIsA_, FamilyAuthority.Member)
                .WhenAsync(TheOtherPlayerTriesToChangeARight)
                .Then(TheHistoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AppointingAKeeperIsLogged()
        {
            await new Spec("Appointing a keeper logs who appointed whom to which rank")
                .Given(TheOtherPlayerIsA_, FamilyAuthority.Member)
                .And(TheFamilyServiceAccepts_, FamilyAuthorityChange.Changed)
                .WhenAsync(TheHeadAppointsTheOtherPlayerKeeper)
                .Then(TheHistoryShouldHoldOnly_With_, FamilyLogType.AuthorityChanged, new[] { "{head}", "2", "{other}" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedAppointmentIsNotLogged()
        {
            await new Spec("An appointment the family refuses is not logged")
                .Given(TheOtherPlayerIsA_, FamilyAuthority.Member)
                .And(TheFamilyServiceAccepts_, FamilyAuthorityChange.NotAuthorized)
                .WhenAsync(TheHeadAppointsTheOtherPlayerKeeper)
                .Then(TheHistoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDailyMessageIsLogged()
        {
            await new Spec("Writing today's message stores it and logs it")
                .WhenAsync(TheHeadWritesTheDailyMessage_, "good morning")
                .Then(TheHistoryShouldHoldOnly_With_, FamilyLogType.DailyMessage, new[] { "{head}", "good morning" })
                .And(TheHeadsDailyMessageShouldBe_, "good morning")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDailyMessageIsWrittenOnceADay()
        {
            await new Spec("A second daily message the same day is refused with the client's once-a-day message")
                .GivenAsync(TheHeadWritesTheDailyMessage_, "good morning")
                .WhenAsync(TheHeadWritesTheDailyMessage_, "good evening")
                .Then(TheHeadShouldBeTold_, Game18NConstString.OnlyOncePerDay)
                .And(TheHeadsDailyMessageShouldBe_, "good morning")
                .And(TheHistoryShouldHoldOnly_With_, FamilyLogType.DailyMessage, new[] { "{head}", "good morning" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDailyMessageIsFreeAgainTheNextDay()
        {
            await new Spec("A day later the daily message can be written again")
                .GivenAsync(TheHeadWritesTheDailyMessage_, "good morning")
                .And(_HoursPass, 25)
                .WhenAsync(TheHeadWritesTheDailyMessage_, "good evening")
                .Then(TheHeadsDailyMessageShouldBe_, "good evening")
                .ExecuteAsync();
        }

        private void TheHeadInvitedTheOtherPlayer() =>
            _head.Character.FamilyRequestCharacterIds[_other.Character.CharacterId] = FamilyId;

        private void TheOtherPlayerIsA_(FamilyAuthority authority)
        {
            var family = FamilyOf((_head.Character.CharacterId, FamilyAuthority.Head), (_other.Character.CharacterId, authority));
            _head.Character.Family = family;
            _other.Character.Family = family;
        }

        private void TheFamilyServiceAccepts_(FamilyAuthorityChange result)
        {
            var headId = _head.Character.CharacterId;
            var otherId = _other.Character.CharacterId;
            _familyService.Setup(s => s.ChangeAuthorityAsync(It.IsAny<GameObject.Services.FamilyService.Family>(),
                    headId, otherId, It.IsAny<FamilyAuthority>()))
                .ReturnsAsync(result);
        }

        private void _HoursPass(int hours) => World.Clock.AdvanceHours(hours);

        private Task TheOtherPlayerAnswers_(FamilyInviteAnswerType type) =>
            World.Create<JoinFamilyPacketHandler>()
                .ExecuteAsync(new JoinFamilyPacket { Type = type, CharacterId = _head.Character.CharacterId }, _other);

        private Task TheOtherPlayerLeaves() =>
            World.Create<FamilyLeavePacketHandler>()
                .ExecuteAsync(new FamilyLeavePacket(), _other);

        private Task TheHeadTriesToLeave() =>
            World.Create<FamilyLeavePacketHandler>()
                .ExecuteAsync(new FamilyLeavePacket(), _head);

        private Task TheHeadDismissesTheOtherPlayer() =>
            World.Create<FamilyDismissPacketHandler>()
                .ExecuteAsync(new FamilyDismissPacket { CharacterName = _other.Character.Name }, _head);

        private Task TheOtherPlayerDismissesTheHead() =>
            World.Create<FamilyDismissPacketHandler>()
                .ExecuteAsync(new FamilyDismissPacket { CharacterName = _head.Character.Name }, _other);

        private Task TheHeadLetsKeepersReadTheHistory() =>
            World.Create<FAuthPacketHandler>()
                .ExecuteAsync(new FAuthPacket { MemberType = Packets.Enumerations.FamilyAuthority.Manager, AuthorityId = 3, Value = 1 }, _head);

        private Task TheOtherPlayerTriesToChangeARight() =>
            World.Create<FAuthPacketHandler>()
                .ExecuteAsync(new FAuthPacket { MemberType = Packets.Enumerations.FamilyAuthority.Manager, AuthorityId = 3, Value = 1 }, _other);

        private Task TheHeadAppointsTheOtherPlayerKeeper() =>
            World.Create<FamilyManagementPacketHandler>()
                .ExecuteAsync(new FamilyManagementPacket
                {
                    TargetId = _other.Character.CharacterId,
                    FamilyAuthorityType = Packets.Enumerations.FamilyAuthority.Manager
                }, _head);

        private Task TheHeadWritesTheDailyMessage_(string message) =>
            World.Create<FamilyTodayPacketHandler>().ExecuteAsync(new FamilyTodayPacket { Message = message }, _head);

        private string Expand(string data) => data
            .Replace("{head}", _head.Character.Name)
            .Replace("{other}", _other.Character.Name);

        private void TheHistoryShouldHoldOnly_With_(FamilyLogType type, string[] arguments)
        {
            var log = _logs.Single();
            Assert.AreEqual(FamilyId, log.FamilyId);
            Assert.AreEqual(type, log.FamilyLogType);
            var expected = arguments.Select(Expand).Cast<string?>()
                .Concat(Enumerable.Repeat<string?>(null, 4 - arguments.Length)).ToList();
            CollectionAssert.AreEqual(expected, new[] { log.FirstArgument, log.SecondArgument, log.ThirdArgument, log.FourthArgument });
        }

        private void TheHistoryShouldBeEmpty() => Assert.IsEmpty(_logs);

        private void TheHeadsDailyMessageShouldBe_(string message) =>
            Assert.AreEqual(message, _head.Character.Family.NotNull().Members.Single(s => s.CharacterId == _head.Character.CharacterId).DailyMessage);

        private void TheHeadShouldBeTold_(Game18NConstString message) =>
            Assert.AreEqual(message, _head.LastPackets.OfType<InfoiPacket>().LastOrDefault()?.Message);
    }
}
