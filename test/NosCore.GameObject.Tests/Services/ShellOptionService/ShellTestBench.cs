//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NosCore.Algorithm.ShellService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.ShellOptionService
{
    // The real shell option service over an in-memory option table, plus helpers to put shells,
    // equipment and consumables in a test character's inventory.
    public sealed class ShellTestBench
    {
        public const short PearlVNum = 1429;
        public const short EraserVNum = 1430;

        public List<EquipmentOptionDto> Options { get; } = new();

        public Mock<IRandomNumberSource> Random { get; } = new();

        public GameObject.Services.ShellOptionService.ShellOptionService Service { get; }

        public ShellTestBench()
        {
            var dao = new Mock<IDao<EquipmentOptionDto, Guid>>();
            dao.Setup(d => d.Where(It.IsAny<Expression<Func<EquipmentOptionDto, bool>>>()))
                .Returns((Expression<Func<EquipmentOptionDto, bool>> p) => Options.Where(p.Compile()).ToList());
            dao.Setup(d => d.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<EquipmentOptionDto>>()))
                .Returns((IEnumerable<EquipmentOptionDto> dtos) =>
                {
                    Options.AddRange(dtos);
                    return Task.FromResult(true);
                });
            dao.Setup(d => d.TryDeleteAsync(It.IsAny<IEnumerable<Guid>>()))
                .Returns((IEnumerable<Guid> ids) =>
                {
                    var keys = ids.ToHashSet();
                    var removed = Options.Where(o => keys.Contains(o.Id)).ToList();
                    Options.RemoveAll(o => keys.Contains(o.Id));
                    return Task.FromResult<IEnumerable<EquipmentOptionDto>?>(removed);
                });
            Service = new GameObject.Services.ShellOptionService.ShellOptionService(new ShellService(), Random.Object, dao.Object);
        }

        public static InventoryItemInstance Shell(ClientSession session, short slot, byte subType, ShellType type, short rare, byte level) =>
            Wearable(session, slot, new Item
            {
                VNum = (short)(565 + subType * 12),
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Shell,
                ItemSubType = subType,
                ShellType = type,
            }, rare, level);

        public static InventoryItemInstance Equipment(ClientSession session, short slot, ItemType itemType, short rare, byte levelMinimum) =>
            Wearable(session, slot, new Item
            {
                VNum = (short)(itemType == ItemType.Weapon ? 299 : 298),
                Type = NoscorePocketType.Equipment,
                ItemType = itemType,
                LevelMinimum = levelMinimum,
            }, rare, 0);

        public static InventoryItemInstance Consumable(ClientSession session, short slot, short vnum, ItemEffectType effect, short amount)
        {
            var item = new Item { VNum = vnum, Type = NoscorePocketType.Main, ItemType = ItemType.Special, Effect = effect };
            var inventoryItem = InventoryItemInstance.Create(new ItemInstanceForTest(vnum) { Amount = amount, Item = item },
                session.Character.CharacterId);
            inventoryItem.Slot = slot;
            inventoryItem.Type = NoscorePocketType.Main;
            session.Character.InventoryService[inventoryItem.ItemInstanceId] = inventoryItem;
            return inventoryItem;
        }

        public void GiveOptions(InventoryItemInstance piece, params (byte Level, byte Type, int Value)[] options) =>
            Options.AddRange(options.Select(o => new EquipmentOptionDto
            {
                Id = Guid.NewGuid(),
                WearableInstanceId = piece.ItemInstanceId,
                Level = o.Level,
                Type = o.Type,
                Value = o.Value,
            }));

        public List<EquipmentOptionDto> OptionsOf(InventoryItemInstance piece) =>
            Options.Where(o => o.WearableInstanceId == piece.ItemInstanceId).ToList();

        private static InventoryItemInstance Wearable(ClientSession session, short slot, Item item, short rare, byte upgrade)
        {
            var wearable = new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Amount = 1,
                Rare = rare,
                Upgrade = upgrade,
            };
            var inventoryItem = InventoryItemInstance.Create(wearable, session.Character.CharacterId);
            inventoryItem.Slot = slot;
            inventoryItem.Type = NoscorePocketType.Equipment;
            session.Character.InventoryService[inventoryItem.ItemInstanceId] = inventoryItem;
            return inventoryItem;
        }

        private sealed class ItemInstanceForTest(short vnum) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
