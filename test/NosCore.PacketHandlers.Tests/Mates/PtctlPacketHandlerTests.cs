//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Mates
{
    [TestClass]
    public class PtctlPacketHandlerTests
    {
        private PtctlPacketHandler _handler = null!;
        private ClientSession _session = null!;
        private ClientSession _otherSession = null!;
        private MapInstance _map = null!;
        private Mate _pet = null!;
        private Mate _partner = null!;
        private Mate _otherPet = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _otherSession = await TestHelpers.Instance.GenerateSessionAsync();
            _map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            _session.Character.MapInstance = _map;
            _otherSession.Character.MapInstance = _map;
            _handler = new PtctlPacketHandler(new MateControlService(TestHelpers.Instance.DistanceCalculator));
        }

        [TestMethod]
        public async Task AnOwnPetWalksToTheOrderedCell()
        {
            await new Spec("An own pet out on the map walks to the cell it is ordered to")
                .GivenAsync(APetOutAt_, (short)5, (short)5)
                .WhenAsync(OrderingTo_, (short)6, (short)4)
                .Then(ThePetShouldStandAt_, (short)6, (short)4)
                .And(TheMapShouldSeeThePetMoveTo_, (short)6, (short)4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryMateInTheOrderMoves()
        {
            await new Spec("A ptctl naming the pet and the partner moves both")
                .GivenAsync(APetOutAt_, (short)5, (short)5)
                .AndAsync(APartnerOutAt_, (short)4, (short)5)
                .WhenAsync(OrderingBothMates)
                .Then(ThePetShouldStandAt_, (short)6, (short)5)
                .And(ThePartnerShouldStandAt_, (short)5, (short)4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherPlayersPetIsIgnored()
        {
            await new Spec("A ptctl naming another player's pet leaves that pet where it is")
                .GivenAsync(AnotherPlayersPetOutAt_, (short)5, (short)5)
                .WhenAsync(OrderingTheOtherPlayersPetTo_, (short)6, (short)4)
                .Then(TheOtherPlayersPetShouldStandAt_, (short)5, (short)5)
                .And(NothingShouldBeBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnwalkableCellIsRefused()
        {
            await new Spec("A pet ordered onto a blocked cell stays put")
                .GivenAsync(APetOutAt_, (short)5, (short)5)
                .WhenAsync(OrderingTo_, (short)2, (short)2)
                .Then(ThePetShouldStandAt_, (short)5, (short)5)
                .And(NothingShouldBeBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACellOutOfReachIsRefused()
        {
            await new Spec("A pet ordered further than one step stays put")
                .GivenAsync(APetOutAt_, (short)5, (short)5)
                .WhenAsync(OrderingTo_, (short)0, (short)0)
                .Then(ThePetShouldStandAt_, (short)5, (short)5)
                .And(NothingShouldBeBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMateKeptInTheMinilandIsIgnored()
        {
            await new Spec("A mate that is not in the team cannot be ordered")
                .Given(APetKeptInTheMiniland)
                .WhenAsync(OrderingTo_, (short)6, (short)4)
                .Then(NothingShouldBeBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHiddenOwnersPetOnlyMovesOnTheOwnersScreen()
        {
            await new Spec("An invisible owner's pet move is sent to the owner alone")
                .GivenAsync(APetOutAt_, (short)5, (short)5)
                .And(TheOwnerIsInvisible)
                .WhenAsync(OrderingTo_, (short)6, (short)4)
                .Then(ThePetShouldStandAt_, (short)6, (short)4)
                .And(NothingShouldBeBroadcast)
                .And(TheOwnerShouldSeeThePetMove)
                .ExecuteAsync();
        }

        private async Task APetOutAt_(short x, short y)
        {
            _pet = await OutMate(_session, TestMates.Pet(_session.Character.CharacterId), x, y);
        }

        private async Task APartnerOutAt_(short x, short y)
        {
            _partner = await OutMate(_session, TestMates.Partner(_session.Character.CharacterId), x, y);
        }

        private async Task AnotherPlayersPetOutAt_(short x, short y)
        {
            _otherPet = await OutMate(_otherSession, TestMates.Pet(_otherSession.Character.CharacterId), x, y);
        }

        private void APetKeptInTheMiniland()
        {
            _pet = TestMates.Pet(_session.Character.CharacterId, isTeamMember: false);
            _session.Character.Mates[_pet.MateTransportId] = _pet;
        }

        private void TheOwnerIsInvisible()
        {
            _session.Character.Invisible = true;
        }

        private async Task<Mate> OutMate(ClientSession owner, Mate mate, short x, short y)
        {
            mate.NpcMonster.Speed = 10;
            owner.Character.Mates[mate.MateTransportId] = mate;
            await mate.SpawnAsync(owner.Character, _map, x, y);
            _map.LastPackets.Clear();
            owner.LastPackets.Clear();
            return mate;
        }

        private Task OrderingTo_(short x, short y) => Order(_session, (_pet.MateTransportId, x, y));

        private Task OrderingTheOtherPlayersPetTo_(short x, short y) =>
            Order(_session, (_otherPet.MateTransportId, x, y));

        private Task OrderingBothMates() =>
            Order(_session, (_pet.MateTransportId, 6, 5), (_partner.MateTransportId, 5, 4));

        private Task Order(ClientSession session, params (long Id, short X, short Y)[] orders) =>
            _handler.ExecuteAsync(new PtctlPacket
            {
                MapType = 1,
                Amount = (short)orders.Length,
                Mates = orders.Select(o => (PtctlSubPacket?)new PtctlSubPacket
                {
                    VisualId = o.Id,
                    PositionX = o.X,
                    PositionY = o.Y
                }).ToList(),
                VisualId = orders[0].Id,
                Speed = 10
            }, session);

        private void ThePetShouldStandAt_(short x, short y) => AssertStandsAt(_pet, x, y);

        private void ThePartnerShouldStandAt_(short x, short y) => AssertStandsAt(_partner, x, y);

        private void TheOtherPlayersPetShouldStandAt_(short x, short y) => AssertStandsAt(_otherPet, x, y);

        private static void AssertStandsAt(Mate mate, short x, short y)
        {
            Assert.AreEqual((x, y), (mate.PositionX, mate.PositionY));
            Assert.AreEqual((x, y), (mate.Entity!.Value.PositionX, mate.Entity.Value.PositionY));
        }

        private void TheMapShouldSeeThePetMoveTo_(short x, short y)
        {
            var move = _map.LastPackets.OfType<MovePacket>().Single();
            Assert.AreEqual(VisualType.Npc, move.VisualType);
            Assert.AreEqual(_pet.MateTransportId, move.VisualEntityId);
            Assert.AreEqual((x, y), (move.MapX, move.MapY));
            Assert.AreEqual(10, move.Speed);
        }

        private void NothingShouldBeBroadcast()
        {
            Assert.IsFalse(_map.LastPackets.OfType<MovePacket>().Any());
        }

        private void TheOwnerShouldSeeThePetMove()
        {
            Assert.IsTrue(_session.LastPackets.OfType<MovePacket>().Any(s => s.VisualEntityId == _pet.MateTransportId));
        }
    }
}
