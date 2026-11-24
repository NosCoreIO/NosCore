//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Data.Enumerations.Group;
using NosCore.PacketHandlers.Chat;
using NosCore.PacketHandlers.Family;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class MutedSpeakerTests
    {
        private TestWorld World;
        private ClientSession _speaker;
        private ClientSession _listener;
        private readonly List<IMessage> _relayed = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.PubSubHub
                .Setup(s => s.SendMessageAsync(It.IsAny<IMessage>()))
                .Callback<IMessage>(_relayed.Add)
                .ReturnsAsync(true);
            _speaker = await World.CreateSessionAsync();
            _listener = await World.CreateSessionAsync();
            _speaker.Character.Family = new GameObject.Services.FamilyService.Family
            {
                FamilyId = 42,
                Name = "Testers",
                Members = new List<FamilyCharacterDto>
                {
                    new() { FamilyId = 42, CharacterId = _speaker.Character.CharacterId, Authority = FamilyAuthority.Head }
                }
            };
            var group = new GameObject.Services.GroupService.Group(GroupType.Group, World.SessionGroupFactory);
            group.JoinGroup(_speaker.Character);
            group.JoinGroup(_listener.Character);
            _speaker.Character.Group = group;
            _listener.Character.Group = group;
        }

        [TestMethod]
        public async Task AMutedPlayerCannotWhisper()
        {
            await MuteAsync(30);

            await World.Create<WhisperPacketHandler>().ExecuteAsync(
                new WhisperPacket { Message = $"{_listener.Character.Name} hello" }, _speaker);

            Assert.IsFalse(_speaker.LastPackets.OfType<SpeakPacket>().Any(), "the whisper is not echoed");
            World.PubSubHub.Verify(s => s.SendMessageAsync(It.IsAny<IMessage>()), Times.Never);
            IsToldTheMinutesLeft(30);
        }

        [TestMethod]
        public async Task AMutedPlayerCannotTalkToTheGroup()
        {
            await MuteAsync(30);

            await World.Create<GroupTalkPacketHandler>().ExecuteAsync(new GroupTalkPacket { Message = "hello" }, _speaker);

            Assert.IsFalse(_speaker.Character.Group.LastPackets.Any());
            IsToldTheMinutesLeft(30);
        }

        [TestMethod]
        public async Task AnUnmutedPlayerTalksToTheGroup()
        {
            await World.Create<GroupTalkPacketHandler>().ExecuteAsync(new GroupTalkPacket { Message = "hello" }, _speaker);

            var said = _speaker.Character.Group.LastPackets.OfType<SpeakPacket>().Single();
            Assert.AreEqual("hello", said.Message);
        }

        [TestMethod]
        public async Task AMutedPlayerCannotTalkToTheFamily()
        {
            await MuteAsync(30);

            await World.Create<FamilyChatPacketHandler>().ExecuteAsync(new FamilyChatPacket { Message = "hello" }, _speaker);

            Assert.AreEqual(0, _relayed.Count);
            IsToldTheMinutesLeft(30);
        }

        [TestMethod]
        public async Task AMutedHeadCannotShoutToTheFamily()
        {
            await MuteAsync(30);

            await World.Create<FamilyShoutPacketHandler>().ExecuteAsync(new FamilyShoutPacket { Message = "hello" }, _speaker);

            Assert.AreEqual(0, _relayed.Count);
            IsToldTheMinutesLeft(30);
        }

        [TestMethod]
        public async Task AMutedPlayerCannotTalkToAFriend()
        {
            await MuteAsync(30);

            await World.Create<BtkPacketHandler>().ExecuteAsync(
                new BtkPacket { CharacterId = _listener.Character.CharacterId, Message = "hello" }, _speaker);

            Assert.IsFalse(_listener.LastPackets.Any());
            World.PubSubHub.Verify(s => s.SendMessageAsync(It.IsAny<IMessage>()), Times.Never);
            IsToldTheMinutesLeft(30);
        }

        [TestMethod]
        public async Task AnEndedMuteLetsTheFamilyHearAgain()
        {
            await MuteAsync(30);
            World.Clock.Advance(Duration.FromMinutes(30));

            await World.Create<FamilyChatPacketHandler>().ExecuteAsync(new FamilyChatPacket { Message = "hello" }, _speaker);

            Assert.AreEqual(1, _relayed.OfType<FamilyChatData>().Count());
        }

        private Task MuteAsync(int minutes)
        {
            var now = World.Clock.GetCurrentInstant();
            return World.PenaltyLogDao.TryInsertOrUpdateAsync(new PenaltyLogDto
            {
                AccountId = _speaker.Account.AccountId,
                Penalty = PenaltyType.Muted,
                DateStart = now,
                DateEnd = now.Plus(Duration.FromMinutes(minutes)),
                AdminName = "admin",
                Reason = "test",
            });
        }

        private void IsToldTheMinutesLeft(long minutes)
        {
            var told = _speaker.LastPackets.OfType<SayiPacket>().Single(p => p.Message == Game18NConstString.RemainingPunishment);
            Assert.AreEqual(SayColorType.Red, told.Type);
            Assert.AreEqual(minutes, told.Game18NArguments[0]);
        }
    }
}
