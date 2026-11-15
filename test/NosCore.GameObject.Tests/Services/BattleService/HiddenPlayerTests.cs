//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class HiddenPlayerTests
    {
        private const short HideCardId = 111;
        private const short OtherCardId = 112;

        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private HiddenHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            World.Freeze<Mock<ICardCatalog>>();
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _handler = World.Create<HiddenHandler>();
        }

        [TestMethod]
        public async Task AHideBuffHidesItsBearer()
        {
            await HideAsync();

            Assert.IsTrue(_session.Character.Invisible);
            Assert.IsTrue(_session.Character.Camouflage);
        }

        [TestMethod]
        public async Task TheHideBuffEndingRevealsTheBearer()
        {
            await HideAsync();

            await _buffs.RemoveAsync(_session.Character, HideCardId);
            await _handler.Handle(new HiddenChangedEvent(_session.Character));

            Assert.IsFalse(_session.Character.Invisible);
            Assert.IsFalse(_session.Character.Camouflage);
        }

        [TestMethod]
        public async Task EndingTheHideEffectLeavesTheOtherBuffs()
        {
            await HideAsync();
            await _buffs.ApplyAsync(_session.Character, new CardDto { CardId = OtherCardId, Duration = 50 },
                new List<BCardDto>(), _session.Character);

            await _buffs.RemoveEffectAsync(_session.Character, BCardEffect.SpecialActionsHide);

            Assert.IsFalse(_buffs.HasBuff(_session.Character, HideCardId));
            Assert.IsTrue(_buffs.HasBuff(_session.Character, OtherCardId));
        }

        private async Task HideAsync()
        {
            await _buffs.ApplyAsync(_session.Character, new CardDto { CardId = HideCardId, Duration = 50 },
                new List<BCardDto> { new() { Type = BCardEffect.SpecialActionsHide.Type(), SubType = BCardEffect.SpecialActionsHide.SubType() } },
                _session.Character);
            await _handler.Handle(new HiddenChangedEvent(_session.Character));
        }
    }
}
