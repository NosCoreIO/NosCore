//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class BlInsPacketHandlerTests
    {
        private TestWorld World;
        private static readonly ILogger<BlInsPackettHandler> Logger = new Mock<ILogger<BlInsPackettHandler>>().Object;
        private BlInsPackettHandler BlInsPacketHandler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();

            World.ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>(){
                    new ChannelInfo
                    {
                      Host = "127.0.0.1",
                      Type = ServerType.WorldServer,
                      Id = 1
                    }
                });
            BlacklistCache = new Mock<IBlacklistCacheService>();
            BlInsPacketHandler = new BlInsPackettHandler(World.BlacklistHub.Object, BlacklistCache.Object, Logger, World.LogLanguageLocalizer);
            World.PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
        }

        [TestMethod]
        public async Task BlacklistingDisconnectedPlayerShouldNotCreateRelation()
        {
            await new Spec("Blacklisting disconnected player should not create relation")
                .WhenAsync(BlacklistingDisconnectedCharacter)
                .ThenAsync(NoBlockedRelationShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlacklistingConnectedPlayerShouldCreateRelation()
        {
            await new Spec("Blacklisting connected player should create relation")
                .GivenAsync(TargetIsOnline)
                .WhenAsync(BlacklistingTargetCharacter)
                .ThenAsync(BlockedRelationShouldExist)
                .And(TheCachedBlacklistShouldBeDropped)
                .ExecuteAsync();
        }

        private ClientSession TargetSession;
        private Mock<IBlacklistCacheService> BlacklistCache;

        private void TheCachedBlacklistShouldBeDropped()
        {
            var visualId = Session.Character.VisualId;
            BlacklistCache.Verify(s => s.Invalidate(visualId), Times.Once);
        }

        private async Task TargetIsOnline()
        {
            TargetSession = await World.GenerateSessionAsync();
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
            var blacklist = World.Create<BlacklistService>();
            World.BlacklistHub.Setup(s => s.AddBlacklistAsync(It.IsAny<BlacklistRequest>()))
                .Returns(blacklist.BlacklistPlayerAsync(Session.Character.CharacterId, TargetSession.Character.VisualId));
        }

        private async Task BlacklistingDisconnectedCharacter()
        {
            await BlInsPacketHandler.ExecuteAsync(new BlInsPacket { CharacterId = 2 }, Session);
        }

        private async Task BlacklistingTargetCharacter()
        {
            await BlInsPacketHandler.ExecuteAsync(new BlInsPacket { CharacterId = TargetSession.Character.CharacterId }, Session);
        }

        private async Task NoBlockedRelationShouldExist()
        {
            var characterId = Session.Character.CharacterId;
            var result = await World.CharacterRelationDao.FirstOrDefaultAsync(s =>
                characterId == s.CharacterId &&
                s.RelationType == CharacterRelationType.Blocked);
            Assert.IsNull(result);
        }

        private async Task BlockedRelationShouldExist()
        {
            var characterId = Session.Character.CharacterId;
            var targetCharacterId = TargetSession.Character.CharacterId;
            var result = await World.CharacterRelationDao.FirstOrDefaultAsync(s =>
                characterId == s.CharacterId &&
                targetCharacterId == s.RelatedCharacterId &&
                s.RelationType == CharacterRelationType.Blocked);
            Assert.IsNotNull(result);
        }
    }
}
