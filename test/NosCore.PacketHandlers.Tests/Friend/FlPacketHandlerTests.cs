//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FlPacketHandlerTests
    {
        private TestWorld World;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao;
        private FlCommandPacketHandler FlPacketHandler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            CharacterRelationDao = World.CharacterRelationDao;
            World.ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>(){
                    new ChannelInfo
                    {
                        Host = "127.0.0.1",
                        Type = ServerType.WorldServer,
                        Id = 1
                    }
                });
            Session = await World.CreateSessionAsync();
            FlPacketHandler = new FlCommandPacketHandler(World.Create<NosCore.GameObject.Services.BroadcastService.SessionRegistry>());
        }

        [TestMethod]
        public async Task AddingDistantFriendShouldCreateRelation()
        {
            await new Spec("Adding distant friend should create relation")
                .GivenAsync(TargetPlayerIsOnlineAndFriendRequestExists)
                .WhenAsync(AddingFriendByName)
                .ThenAsync(FriendRelationShouldExist)
                .ExecuteAsync();
        }

        private ClientSession TargetSession;

        private async Task TargetPlayerIsOnlineAndFriendRequestExists()
        {
            TargetSession = await World.CreateSessionAsync();
            var friendRequestHolder = World.Create<FriendRequestRegistry>();
            friendRequestHolder.RegisterRequest(Guid.NewGuid(),
                TargetSession.Character.CharacterId, Session.Character.CharacterId);
            World.PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = TargetSession.Character.CharacterId }
                    },
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            var friend = new FriendService(NullLogger<FriendService>.Instance, CharacterRelationDao, World.CharacterDao,
                friendRequestHolder, World.PubSubHub.Object, World.ChannelHub.Object, World.LogLanguageLocalizer);
            World.FriendHub.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(Session.Character.CharacterId,
                    TargetSession.Character.VisualId,
                    FinsPacketType.Accepted));
        }

        private async Task AddingFriendByName()
        {
            await FlPacketHandler.ExecuteAsync(new FlCommandPacket { CharacterName = TargetSession.Character.Name }, Session);
        }

        private async Task FriendRelationShouldExist()
        {
            var characterId = Session.Character.CharacterId;
            var targetCharacterId = TargetSession.Character.CharacterId;
            var result = await CharacterRelationDao.FirstOrDefaultAsync(s =>
                s.CharacterId == characterId &&
                s.RelatedCharacterId == targetCharacterId &&
                s.RelationType == CharacterRelationType.Friend);
            Assert.IsNotNull(result);
        }
    }
}
