//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class SpeedBoostEndHandlerTests
    {
        private const short MountVNum = 9149;
        private const short BoostCardId = 336;
        private const short EndCardId = 786;
        private const short OtherCardId = 117;

        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private SpeedBoostEndHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.IsGrantedBy(BoostCardId, ItemEffectType.SpeedBooster)).Returns(true);
            cards.Setup(c => c.GetCard(EndCardId)).Returns(new CardDto { CardId = EndCardId, BuffType = BuffType.Bad, Duration = 10 });
            cards.Setup(c => c.GetCardBCards(EndCardId)).Returns(new List<BCardDto>
            {
                new() { Type = BCardEffect.MoveMovementImpossible.Type(), SubType = BCardEffect.MoveMovementImpossible.SubType() },
            });
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _handler = World.Create<SpeedBoostEndHandler>();
        }

        [TestMethod]
        public async Task WhenTheBoostRunsOutTheMountsCardIsGivenForItsOwnTime()
        {
            Ride(new ItemDto { VNum = MountVNum, Speed = 20, SpeedBoost = 3, SpeedBoostDuration = 9, SpeedBoostEndCardId = EndCardId });

            await _handler.Handle(new BuffExpiredEvent(_session.Character, BoostCardId));

            var buff = _buffs.GetActiveBuffs(_session.Character).Single();
            Assert.AreEqual(EndCardId, buff.CardId);
            Assert.AreEqual(1, (int)(buff.ExpiresAt - World.Clock.GetCurrentInstant()).TotalSeconds);
            Assert.IsTrue(_buffs.HasEffect(_session.Character, BCardEffect.MoveMovementImpossible));
        }

        [TestMethod]
        public async Task AMountWithNothingAtTheEndOfItsBoostGivesNothing()
        {
            Ride(new ItemDto { VNum = MountVNum, Speed = 20, SpeedBoost = 2, SpeedBoostDuration = 3 });

            await _handler.Handle(new BuffExpiredEvent(_session.Character, BoostCardId));

            Assert.AreEqual(0, _buffs.GetActiveBuffs(_session.Character).Count);
        }

        [TestMethod]
        public async Task AnotherCardRunningOutGivesNothing()
        {
            Ride(new ItemDto { VNum = MountVNum, Speed = 20, SpeedBoost = 3, SpeedBoostDuration = 9, SpeedBoostEndCardId = EndCardId });

            await _handler.Handle(new BuffExpiredEvent(_session.Character, OtherCardId));

            Assert.AreEqual(0, _buffs.GetActiveBuffs(_session.Character).Count);
        }

        [TestMethod]
        public async Task ARiderWhoGotOffBeforeTheEndGetsNothing()
        {
            Ride(new ItemDto { VNum = MountVNum, Speed = 20, SpeedBoost = 3, SpeedBoostDuration = 9, SpeedBoostEndCardId = EndCardId });
            _session.Character.IsVehicled = false;
            _session.Character.VehicleVNum = 0;

            await _handler.Handle(new BuffExpiredEvent(_session.Character, BoostCardId));

            Assert.AreEqual(0, _buffs.GetActiveBuffs(_session.Character).Count);
        }

        private void Ride(ItemDto mount)
        {
            World.ItemList.RemoveAll(i => i.VNum == mount.VNum);
            World.ItemList.Add(mount);
            _session.Character.IsVehicled = true;
            _session.Character.VehicleSpeed = mount.Speed;
            _session.Character.VehicleVNum = mount.VNum;
        }
    }
}
