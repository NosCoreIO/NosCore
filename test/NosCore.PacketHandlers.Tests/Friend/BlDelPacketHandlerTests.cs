//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BlackListService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class BlDelPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private BlacklistService BlackListController = null!;
        private Mock<IBlacklistHub> BlackListHttpClient = null!;
        private BlDelPacketHandler BlDelPacketHandler = null!;
        private Mock<IDao<CharacterDto, long>> CharacterDao = null!;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao = null!;
        private Mock<IPubSubHub> ConnectedAccountHttpClient = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            CharacterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ConnectedAccountHttpClient = TestHelpers.Instance.PubSubHub;
            ChannelHub = new Mock<IChannelHub>();
            ConnectedAccountHttpClient.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            BlackListHttpClient = TestHelpers.Instance.BlacklistHttpClient;
            BlDelPacketHandler = new BlDelPacketHandler(BlackListHttpClient.Object, TestHelpers.Instance.GameLanguageLocalizer);
            CharacterDao = new Mock<IDao<CharacterDto, long>>();
            BlackListController = new BlacklistService(ConnectedAccountHttpClient.Object, ChannelHub.Object, CharacterRelationDao,
                CharacterDao.Object);
            BlackListHttpClient.Setup(s => s.GetBlacklistedAsync(It.IsAny<long>()))
                .Returns((long id) => BlackListController.GetBlacklistedListAsync(id));
            BlackListHttpClient.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
                .Callback((Guid id) => Task.FromResult(BlackListController.UnblacklistAsync(id)));
        }

        [TestMethod]
        public async Task DeletingBlacklistedPlayerWhenDisconnectedShouldSucceed()
        {
            await new Spec("Deleting blacklisted player when disconnected should succeed")
                .Given(TargetPlayerIsDisconnected)
                .AndAsync(CharacterHasBlockedDisconnectedTarget)
                .WhenAsync(DeletingFromBlacklist)
                .Then(BlacklistShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingBlacklistedPlayerShouldSucceed()
        {
            await new Spec("Deleting blacklisted player should succeed")
                .GivenAsync(TargetPlayerIsOnline)
                .AndAsync(CharacterHasBlockedTarget)
                .WhenAsync(DeletingTargetFromBlacklist)
                .Then(BlacklistShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingNonBlacklistedPlayerShouldShowError()
        {
            await new Spec("Deleting non blacklisted player should show error")
                .GivenAsync(TargetPlayerIsOnline)
                .WhenAsync(DeletingTargetFromBlacklist)
                .Then(ShouldReceiveNotInBlacklistMessage)
                .ExecuteAsync();
        }

        private ClientSession? TargetSession;
        private long TargetCharacterId = 2;

        private void TargetPlayerIsDisconnected()
        {
            var list = new List<CharacterDto>
            {
                Session.Character,
                new() { CharacterId = 2, Name = "test" }
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult(list.FirstOrDefault(exp.Compile()))!);
        }

        private async Task TargetPlayerIsOnline()
        {
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            TargetCharacterId = TargetSession.Character.CharacterId;
            var list = new List<CharacterDto>
            {
                Session.Character,
                TargetSession.Character
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult(list.FirstOrDefault(exp.Compile()))!);
        }

        private async Task CharacterHasBlockedDisconnectedTarget()
        {
            var targetGuid = Guid.NewGuid();
            await CharacterRelationDao.TryInsertOrUpdateAsync(new[]
            {
                new CharacterRelationDto
                {
                    RelatedCharacterId = 2,
                    CharacterRelationId = targetGuid,
                    CharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Blocked
                }
            });
        }

        private async Task CharacterHasBlockedTarget()
        {
            var targetGuid = Guid.NewGuid();
            await CharacterRelationDao.TryInsertOrUpdateAsync(new[]
            {
                new CharacterRelationDto
                {
                    RelatedCharacterId = TargetSession!.Character.CharacterId,
                    CharacterRelationId = targetGuid,
                    CharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Blocked
                }
            });
        }

        private async Task DeletingFromBlacklist()
        {
            await BlDelPacketHandler.ExecuteAsync(new BlDelPacket { CharacterId = 2 }, Session);
        }

        private async Task DeletingTargetFromBlacklist()
        {
            await BlDelPacketHandler.ExecuteAsync(new BlDelPacket { CharacterId = TargetCharacterId }, Session);
        }

        private void BlacklistShouldBeEmpty()
        {
            Assert.IsFalse(CharacterRelationDao.LoadAll().Any());
        }

        private void ShouldReceiveNotInBlacklistMessage()
        {
            var lastpacket = (InfoPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(TestHelpers.Instance.GameLanguageLocalizer[LanguageKey.NOT_IN_BLACKLIST, Session.Account.Language], lastpacket!.Message);
        }
    }
}
