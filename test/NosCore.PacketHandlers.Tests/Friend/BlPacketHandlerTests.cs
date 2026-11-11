//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BlackListService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class BlPacketHandlerTests
    {
        private TestWorld World;
        private static readonly ILogger<NosCore.GameObject.Services.BroadcastService.SessionRegistry> Logger = new Mock<ILogger<NosCore.GameObject.Services.BroadcastService.SessionRegistry>>().Object;
        private BlPacketHandler BlPacketHandler;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            CharacterRelationDao = World.CharacterRelationDao;
            Session = await World.CreateSessionAsync();

            World.ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>(){
                    new ChannelInfo
                    {
                        Host = "127.0.0.1",
                        Type = ServerType.WorldServer,
                        Id = 1
                    }
                });
            World.PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            BlPacketHandler = new BlPacketHandler(new NosCore.GameObject.Services.BroadcastService.SessionRegistry(Logger));
        }

        [TestMethod]
        public async Task BlacklistingDistantPlayerByNameShouldSucceed()
        {
            await new Spec("Blacklisting distant player by name should succeed")
                .GivenAsync(TargetPlayerIsOnline)
                .WhenAsync(BlacklistingTargetByName)
                .ThenAsync(BlockedRelationShouldExist)
                .ExecuteAsync();
        }

        private ClientSession TargetSession;

        private async Task TargetPlayerIsOnline()
        {
            TargetSession = await World.CreateSessionAsync();
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
            var blacklist = new BlacklistService(World.PubSubHub.Object, World.ChannelHub.Object,
                CharacterRelationDao, World.CharacterDao);
            World.BlacklistHub.Setup(s => s.AddBlacklistAsync(It.IsAny<BlacklistRequest>()))
                .Returns(blacklist.BlacklistPlayerAsync(Session.Character.CharacterId, TargetSession.Character.VisualId));
        }

        private async Task BlacklistingTargetByName()
        {
            await BlPacketHandler.ExecuteAsync(new BlPacket { CharacterName = TargetSession.Character.Name }, Session);
        }

        private async Task BlockedRelationShouldExist()
        {
            var characterId = Session.Character.CharacterId;
            var targetCharacterId = TargetSession.Character.CharacterId;
            var result = await CharacterRelationDao.FirstOrDefaultAsync(s =>
                s.CharacterId == characterId &&
                s.RelatedCharacterId == targetCharacterId &&
                s.RelationType == CharacterRelationType.Blocked);
            Assert.IsNotNull(result);
        }
    }
}
