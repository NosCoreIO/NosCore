//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.MapItem
{
    [TestClass]
    public class GoldDropHandlerTests
    {
        private const short GoldVNum = 1046;
        private const short GoldAmount = 1000;
        private ClientSession _session;
        private GoldDropHandler _handler;
        private MapItemPickedUpEvent _event;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            var gold = new Item
            {
                VNum = GoldVNum,
                Type = NoscorePocketType.Main,
                Name = new I18NString { [RegionType.EN] = "Gold" }
            };
            _handler = new GoldDropHandler(TestHelpers.Instance.WorldConfiguration, new List<ItemDto> { gold });
            var mapItem = TestHelpers.Instance.MapItemProvider.NotNull().Create(_session.Character.MapInstance,
                new ItemInstance(gold) { Amount = GoldAmount }, _session.Character.PositionX, _session.Character.PositionY);
            _event = new MapItemPickedUpEvent(_session, mapItem.VisualId, GoldVNum, GoldAmount, mapItem.ItemInstance,
                new GetPacket { PickerType = VisualType.Player, VisualId = mapItem.VisualId });
        }

        [TestMethod]
        public async Task TheSameGoldPickedUpTwiceIsGrantedOnce()
        {
            await new Spec("The same gold picked up twice is granted once")
                .Given(TheCharacterHasNoGold)
                .WhenAsync(TheGoldIsPickedUpTwice)
                .Then(TheCharacterHas_Gold, (long)GoldAmount)
                .ExecuteAsync();
        }

        private void TheCharacterHasNoGold() => _session.Character.Gold = 0;

        private async Task TheGoldIsPickedUpTwice()
        {
            await _handler.Handle(_event);
            await _handler.Handle(_event);
        }

        private void TheCharacterHas_Gold(long gold) => Assert.AreEqual(gold, _session.Character.Gold);
    }
}
