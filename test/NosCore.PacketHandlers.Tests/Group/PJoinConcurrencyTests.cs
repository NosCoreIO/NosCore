//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using GroupOfPlayers = NosCore.GameObject.Services.GroupService.Group;

namespace NosCore.PacketHandlers.Tests.Group
{
    [TestClass]
    public class PJoinConcurrencyTests
    {
        private TestWorld World;
        private const int Contenders = 6;

        private readonly List<ClientSession> _sessions = new();
        private PjoinPacketHandler _handler;
        private ISessionGroupFactory _groups;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            for (var i = 0; i < Contenders + 2; i++)
            {
                _sessions.Add(await World.CreateSessionAsync());
            }

            var groups = new Mock<ISessionGroupFactory>();
            groups.Setup(g => g.Create()).Returns(() => new Mock<ISessionGroup>().Object);
            _groups = groups.Object;
            _handler = new PjoinPacketHandler(NullLogger<PjoinPacketHandler>.Instance, new Mock<IBlacklistHub>().Object,
                World.Clock, new IdService<GroupOfPlayers>(1), World.LogLanguageLocalizer,
                World.GameLanguageLocalizer, World.SessionRegistry, new Mock<GameObject.Services.GroupService.ICoupleBuffService>().Object);
        }

        private void EveryoneAlone()
        {
            foreach (var session in _sessions)
            {
                session.Character.Group = new GroupOfPlayers(GroupType.Group, _groups);
                session.Character.Group.JoinGroup(session.Character);
                session.Character.GroupRequestCharacterIds.Clear();
            }
        }

        private void Invites(ClientSession inviter, ClientSession invited) =>
            inviter.Character.GroupRequestCharacterIds.TryAdd(invited.Character.CharacterId, invited.Character.CharacterId);

        private Task AcceptsAsync(ClientSession invited, ClientSession inviter) =>
            _handler.ExecuteAsync(new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = inviter.Character.CharacterId,
            }, invited);

        private async Task EveryoneAcceptsAtOnceAsync(ClientSession inviter, IEnumerable<ClientSession> invited)
        {
            var accepting = invited.ToList();
            using var start = new Barrier(accepting.Count);
            await Task.WhenAll(accepting.Select(session => Task.Run(() =>
            {
                start.SignalAndWait();
                return AcceptsAsync(session, inviter);
            })));
        }

        [TestMethod]
        public async Task AcceptsAtOnceNeverOverfillAGroup()
        {
            for (var round = 0; round < 200; round++)
            {
                EveryoneAlone();
                var leader = _sessions[0];
                Invites(leader, _sessions[1]);
                await AcceptsAsync(_sessions[1], leader);
                var contenders = _sessions.Skip(2).ToList();
                contenders.ForEach(contender => Invites(leader, contender));

                await EveryoneAcceptsAtOnceAsync(leader, contenders);

                Assert.AreEqual((int)GroupType.Group, leader.Character.Group.Count, $"round {round}");
            }
        }

        [TestMethod]
        public async Task AcceptsAtOncePutTheInviterInOneGroup()
        {
            for (var round = 0; round < 200; round++)
            {
                EveryoneAlone();
                var leader = _sessions[0];
                var contenders = _sessions.Skip(1).ToList();
                contenders.ForEach(contender => Invites(leader, contender));

                await EveryoneAcceptsAtOnceAsync(leader, contenders);

                var groupsHoldingTheLeader = _sessions.Skip(1)
                    .Select(s => s.Character.Group)
                    .Distinct()
                    .Count(g => g.ContainsKey(new Tuple<NosCore.Shared.Enumerations.VisualType, long>(
                        leader.Character.VisualType, leader.Character.VisualId)));
                Assert.AreEqual(1, groupsHoldingTheLeader, $"round {round}");
                Assert.IsTrue(leader.Character.Group.Count <= (int)GroupType.Group, $"round {round}");
            }
        }
    }
}
