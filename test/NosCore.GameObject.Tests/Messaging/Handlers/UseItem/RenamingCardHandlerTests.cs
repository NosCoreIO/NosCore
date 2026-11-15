//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class RenamingCardHandlerTests
    {
        private TestWorld World;
        private const short RenamingCardVNum = 5787;
        private ClientSession _session;
        private RenamingCardHandler _handler;
        private InventoryItemInstance _item;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<RenamingCardHandler>();
        }

        [TestMethod]
        public async Task WrongEffectIsIgnored()
        {
            await new Spec("An item with another effect neither asks nor flags the rename")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.Teleport)
                .WhenAsync(UsingTheItemWithMode_, (byte)1)
                .Then(RenameShouldBeFlagged_, false)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FirstUseAsksTheClientsQuestion()
        {
            await new Spec("The first use asks before anything changes")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.RenamingCard)
                .WhenAsync(UsingTheItemWithMode_, (byte)0)
                .Then(RenameShouldBeFlagged_, false)
                .And(ItemStackCountShouldBe_, (short)1)
                .And(TheRenameQuestionShouldBeAsked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingFlagsTheRenameAndUsesTheCard()
        {
            await new Spec("Confirming flags the character for a rename at the next login and uses the card")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.RenamingCard)
                .WhenAsync(UsingTheItemWithMode_, (byte)1)
                .Then(RenameShouldBeFlagged_, true)
                .And(ItemStackCountShouldBe_, (short)0)
                .And(Message_ShouldBeSent, Game18NConstString.ToChangeYourCharacterNameYouHaveToLogoutAndGoBackToInitial)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AlreadyFlaggedCharacterKeepsTheCard()
        {
            await new Spec("A character already waiting for a rename is refused and keeps the card")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.RenamingCard)
                .And(CharacterIsAlreadyFlagged)
                .WhenAsync(UsingTheItemWithMode_, (byte)1)
                .Then(ItemStackCountShouldBe_, (short)1)
                .And(Message_ShouldBeSent, Game18NConstString.YouHaveAlreadyUsedThisItemToRenameThisCharacter)
                .ExecuteAsync();
        }

        private void ItemInInventoryWithEffect_(ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = RenamingCardVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
            };
            var inst = new ItemInstanceForTest(RenamingCardVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void CharacterIsAlreadyFlagged() => _session.Character.CharacterDto.ShouldRename = true;

        private async Task UsingTheItemWithMode_(byte mode)
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = mode,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void RenameShouldBeFlagged_(bool expected) =>
            Assert.AreEqual(expected, _session.Character.CharacterDto.ShouldRename);

        private void ItemStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(RenamingCardVNum));

        private void Message_ShouldBeSent(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));

        private void TheRenameQuestionShouldBeAsked()
        {
            var qna = _session.LastPackets.OfType<QnaiPacket>().SingleOrDefault();
            Assert.IsNotNull(qna);
            Assert.AreEqual(Game18NConstString.ThisArticleAllowsYouToChangeYourName, qna.Question);
            Assert.AreEqual((byte)1, ((UseItemPacket)qna.YesPacket.NotNull()).Mode);
        }

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
