//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Core;
using NosCore.Shared.Enumerations;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FDelPacketHandlerTests
    {
        private TestWorld World;
        private Mock<IChannelHub> ChannelHttpClient;
        private Mock<IDao<CharacterDto, long>> CharacterDao;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao;
        private Mock<IPubSubHub> ConnectedAccountHttpClient;
        private Mock<IChannelHub> ChannelHub;
        private FdelPacketHandler FDelPacketHandler;
        private FriendService FriendController;
        private Mock<IFriendHub> FriendHttpClient;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            CharacterRelationDao = World.CharacterRelationDao;
            Session = await World.CreateSessionAsync();
            ChannelHttpClient = World.ChannelHub;
            ConnectedAccountHttpClient = World.PubSubHub;
            ChannelHub = new Mock<IChannelHub>();
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo> { new() { Host = "127.0.0.1", Type = ServerType.WorldServer, Id = 1 } });
            ConnectedAccountHttpClient.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            FriendHttpClient = World.FriendHub;
            FDelPacketHandler = new FdelPacketHandler(FriendHttpClient.Object, ChannelHttpClient.Object,
                World.PubSubHub.Object, World.GameLanguageLocalizer, World.Create<NosCore.GameObject.Services.BroadcastService.SessionRegistry>());
            CharacterDao = new Mock<IDao<CharacterDto, long>>();
            FriendController = new FriendService(NullLogger<FriendService>.Instance, CharacterRelationDao, CharacterDao.Object,
                World.Create<FriendRequestRegistry>(), ConnectedAccountHttpClient.Object, ChannelHub.Object, World.LogLanguageLocalizer);
            FriendHttpClient.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .Returns((long id) => FriendController.GetFriendsAsync(id));
            FriendHttpClient.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
                .Callback((Guid id) => Task.FromResult(FriendController.DeleteAsync(id)));
        }

        [TestMethod]
        public async Task DeletingFriendWhenDisconnectedShouldSucceed()
        {
            await new Spec("Deleting friend when disconnected should succeed")
                .GivenAsync(TargetIsDisconnectedWithFriendRelation)
                .WhenAsync(DeletingFriend)
                .Then(NoRelationsShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingFriendShouldSucceed()
        {
            await new Spec("Deleting friend should succeed")
                .GivenAsync(TargetIsOnlineWithFriendRelation)
                .WhenAsync(DeletingTargetFriend)
                .Then(NoRelationsShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingNonFriendShouldShowError()
        {
            await new Spec("Deleting non friend should show error")
                .GivenAsync(TargetIsOnline)
                .WhenAsync(DeletingTargetFriend)
                .Then(ShouldReceiveNotInFriendlistMessage)
                .ExecuteAsync();
        }

        private ClientSession TargetSession;
        private long TargetCharacterId = 2;

        private async Task TargetIsDisconnectedWithFriendRelation()
        {
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                Session.Character.CharacterDto,
                new() { CharacterId = 2, Name = "test" }
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult<CharacterDto?>(list.FirstOrDefault(exp.Compile())));
            await CharacterRelationDao.TryInsertOrUpdateAsync(new[]
            {
                new CharacterRelationDto
                {
                    CharacterId = 2,
                    CharacterRelationId = guid,
                    RelatedCharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                },
                new CharacterRelationDto
                {
                    RelatedCharacterId = 2,
                    CharacterRelationId = targetGuid,
                    CharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                }
            });
        }

        private async Task TargetIsOnlineWithFriendRelation()
        {
            TargetSession = await World.CreateSessionAsync();
            TargetCharacterId = TargetSession.Character.CharacterId;
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                Session.Character.CharacterDto,
                TargetSession.Character.CharacterDto
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult<CharacterDto?>(list.FirstOrDefault(exp.Compile())));
            await CharacterRelationDao.TryInsertOrUpdateAsync(new[]
            {
                new CharacterRelationDto
                {
                    CharacterId = TargetSession.Character.CharacterId,
                    CharacterRelationId = guid,
                    RelatedCharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                },
                new CharacterRelationDto
                {
                    RelatedCharacterId = TargetSession.Character.CharacterId,
                    CharacterRelationId = targetGuid,
                    CharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                }
            });
        }

        private async Task TargetIsOnline()
        {
            TargetSession = await World.CreateSessionAsync();
            TargetCharacterId = TargetSession.Character.CharacterId;
            var list = new List<CharacterDto>
            {
                Session.Character.CharacterDto,
                TargetSession.Character.CharacterDto
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult<CharacterDto?>(list.FirstOrDefault(exp.Compile())));
        }

        private async Task DeletingFriend()
        {
            await FDelPacketHandler.ExecuteAsync(new FdelPacket { CharacterId = 2 }, Session);
        }

        private async Task DeletingTargetFriend()
        {
            await FDelPacketHandler.ExecuteAsync(new FdelPacket { CharacterId = TargetCharacterId }, Session);
        }

        private void NoRelationsShouldExist()
        {
            Assert.IsFalse(CharacterRelationDao.LoadAll().Any());
        }

        private void ShouldReceiveNotInFriendlistMessage()
        {
            var lastpacket = (InfoPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(World.GameLanguageLocalizer[LanguageKey.NOT_IN_FRIENDLIST, Session.Account.Language], lastpacket?.Message);
        }
    }
}
