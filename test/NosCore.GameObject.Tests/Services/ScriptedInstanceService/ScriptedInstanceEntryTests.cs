//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Tests.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class ScriptedInstanceEntryTests
    {
        private TestWorld World;
        private const short SeedVnum = 1012;

        private ClientSession _session;
        private IItemGenerationService _items;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _items = new ItemGenerationService(
                new List<ItemDto> { new() { VNum = SeedVnum, Type = NoscorePocketType.Etc } },
                NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
        }

        private static ScriptedInstance Needing(short amount) => new()
        {
            Definition = new ScriptedInstanceDefinition
            {
                RequiredItems = [new InstanceGift(SeedVnum, amount)]
            }
        };

        private void Give(short amount)
        {
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(SeedVnum, amount), _session.Character.CharacterId));
        }

        private int Held() => _session.Character.InventoryService.Values
            .Where(s => s.ItemInstance.ItemVNum == SeedVnum)
            .Sum(s => s.ItemInstance.Amount);

        [TestMethod]
        public void NothingRequiredLetsAnyoneIn()
        {
            Assert.IsNull(_session.Character.MissingRequiredItem(new ScriptedInstance()));
        }

        [TestMethod]
        public void TooFewSeedsKeepTheDoorShut()
        {
            Give(1);

            Assert.AreEqual(SeedVnum, _session.Character.MissingRequiredItem(Needing(2))?.VNum);
        }

        [TestMethod]
        public async Task EnteringTakesExactlyWhatTheDoorAsksFor()
        {
            Give(5);
            var entrance = Needing(2);

            Assert.IsNull(_session.Character.MissingRequiredItem(entrance));
            await _session.Character.TakeRequiredItemsAsync(entrance);

            Assert.AreEqual(3, Held());
        }
    }
}
