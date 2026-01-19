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
using Serilog;
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
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private BlPacketHandler BlPacketHandler = null!;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            CharacterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            TestHelpers.Instance.ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>(){
                    new ChannelInfo
                    {
                        Type = ServerType.WorldServer,
                        Id = 1
                    }
                });
            TestHelpers.Instance.PubSubHub.Setup(s => s.GetSubscribersAsync())
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

        private ClientSession? TargetSession;

        private async Task TargetPlayerIsOnline()
        {
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            TestHelpers.Instance.PubSubHub.Setup(s => s.GetSubscribersAsync())
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
            var blacklist = new BlacklistService(TestHelpers.Instance.PubSubHub.Object, TestHelpers.Instance.ChannelHub.Object,
                CharacterRelationDao, TestHelpers.Instance.CharacterDao);
            TestHelpers.Instance.BlacklistHttpClient.Setup(s => s.AddBlacklistAsync(It.IsAny<BlacklistRequest>()))
                .Returns(blacklist.BlacklistPlayerAsync(Session.Character.CharacterId, TargetSession.Character.VisualId));
        }

        private async Task BlacklistingTargetByName()
        {
            await BlPacketHandler.ExecuteAsync(new BlPacket { CharacterName = TargetSession!.Character.Name }, Session);
        }

        private async Task BlockedRelationShouldExist()
        {
            var result = await CharacterRelationDao.FirstOrDefaultAsync(s =>
                s.CharacterId == Session.Character.CharacterId &&
                s.RelatedCharacterId == TargetSession!.Character.CharacterId &&
                s.RelationType == CharacterRelationType.Blocked);
            Assert.IsNotNull(result);
        }
    }
}
