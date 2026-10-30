//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.UpgradeService;

// Cellons add a permanent stat option to a piece of jewelry instead of raising its upgrade
// level. The cellon carries the option tier in Item.EffectValue, while the jewel caps both how
// many options it can hold (Item.MaxCellon) and how strong they may be (Item.MaxCellonLvl).
//
// Every attempt costs the cellon and the gold. It rolls one option type out of everything the
// tier offers; landing on a type the jewel already carries is the failure, so the odds fall as
// the jewel fills up.
[UsedImplicitly]
public sealed class CellonOperation(
    IRandomNumberSource random,
    IGameLanguageLocalizer localizer,
    IDao<EquipmentOptionDto, Guid> equipmentOptionDao)
    : UpgradeOperation(random, localizer)
{
    private static readonly long[] GoldCostByCellonLevel =
        { 0, 700, 1400, 3000, 5000, 10000, 20000, 32000, 58000, 95000, 134900 };

    private static readonly CellonOption[][] OptionsByCellonLevel =
    {
        Array.Empty<CellonOption>(),
        new CellonOption[] { new(CellonType.Hp, 30, 100), new(CellonType.Mp, 50, 120), new(CellonType.HpRecovery, 5, 10), new(CellonType.MpRecovery, 8, 15) },
        new CellonOption[] { new(CellonType.Hp, 120, 200), new(CellonType.Mp, 150, 250), new(CellonType.HpRecovery, 14, 20), new(CellonType.MpRecovery, 16, 25) },
        new CellonOption[] { new(CellonType.Hp, 220, 300), new(CellonType.Mp, 280, 330), new(CellonType.HpRecovery, 22, 28), new(CellonType.MpRecovery, 28, 35) },
        new CellonOption[] { new(CellonType.Hp, 330, 400), new(CellonType.Mp, 350, 420), new(CellonType.HpRecovery, 30, 38), new(CellonType.MpRecovery, 38, 45) },
        new CellonOption[] { new(CellonType.Hp, 430, 550), new(CellonType.Mp, 450, 550), new(CellonType.HpRecovery, 40, 50), new(CellonType.MpRecovery, 50, 60) },
        new CellonOption[] { new(CellonType.Hp, 600, 750), new(CellonType.Mp, 600, 750), new(CellonType.HpRecovery, 55, 70), new(CellonType.MpRecovery, 65, 80), new(CellonType.MpConsumption, 1, 7), new(CellonType.CriticalDamageDecrease, 1, 10) },
        new CellonOption[] { new(CellonType.Hp, 800, 1000), new(CellonType.Mp, 800, 1000), new(CellonType.HpRecovery, 75, 90), new(CellonType.MpRecovery, 75, 90), new(CellonType.MpConsumption, 8, 12), new(CellonType.CriticalDamageDecrease, 11, 20) },
        new CellonOption[] { new(CellonType.Hp, 1000, 1300), new(CellonType.Mp, 1000, 1300), new(CellonType.HpRecovery, 100, 120), new(CellonType.MpRecovery, 100, 120), new(CellonType.MpConsumption, 13, 17), new(CellonType.CriticalDamageDecrease, 21, 35) },
        new CellonOption[] { new(CellonType.Hp, 1100, 1500), new(CellonType.Mp, 1100, 1500), new(CellonType.HpRecovery, 110, 135), new(CellonType.MpRecovery, 110, 135), new(CellonType.MpConsumption, 14, 21), new(CellonType.CriticalDamageDecrease, 22, 45) },
        new CellonOption[] { new(CellonType.Hp, 1200, 1700), new(CellonType.Mp, 1200, 1700), new(CellonType.HpRecovery, 120, 150), new(CellonType.MpRecovery, 120, 150), new(CellonType.MpConsumption, 15, 25), new(CellonType.CriticalDamageDecrease, 23, 55) },
    };

    public override UpgradePacketType Kind => UpgradePacketType.CellonItem;

    protected override Game18NConstString SuccessMessage => Game18NConstString.OptionAdded;

    protected override Game18NConstString FailureMessage => Game18NConstString.CellonDisapearedFailedUpgrade;

    protected override IReadOnlyList<IPacket>? TryReject(ClientSession session, UpgradePacket packet)
    {
        if (!TryResolve(session, packet, out var jewelSlot, out var cellonSlot))
        {
            return null;
        }

        var jewel = jewelSlot.ItemInstance!.Item;
        if (cellonSlot.ItemInstance!.Item.EffectValue > jewel.MaxCellonLvl)
        {
            return Reject(Game18NConstString.CellonLevelHigherThanAccessory);
        }

        return LoadOptionTypes(jewelSlot).Count >= jewel.MaxCellon
            ? Reject(Game18NConstString.CantAddMoreOptions)
            : null;
    }

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        if (!TryResolve(session, packet, out var jewelSlot, out var cellonSlot))
        {
            return null;
        }

        var level = cellonSlot.ItemInstance!.Item.EffectValue;
        return new UpgradeContext(
            Source: jewelSlot,
            Target: cellonSlot,
            GoldCost: GoldCostByCellonLevel[level],
            MaterialCosts: Array.Empty<MaterialCost>(),
            ExtraData: new CellonRollData(level, LoadOptionTypes(jewelSlot)));
    }

    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var data = (CellonRollData)ctx.ExtraData!;
        var options = OptionsByCellonLevel[data.Level];
        data.Option = options[Math.Min((int)(roll * options.Length), options.Length - 1)];
        return data.Taken.Contains((byte)data.Option.Type) ? UpgradeOutcome.Failure : UpgradeOutcome.Success;
    }

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var data = (CellonRollData)ctx.ExtraData!;
        var jewel = (WearableInstance)ctx.Source.ItemInstance!;
        var option = data.Option!;

        data.Rolled = new EquipmentOptionDto
        {
            Id = Guid.NewGuid(),
            WearableInstanceId = ctx.Source.ItemInstanceId,
            Level = (byte)data.Level,
            Type = (byte)option.Type,
            Value = option.Minimum + Roll(option.Maximum - option.Minimum + 1),
        };
        jewel.Cellon = (byte)(data.Taken.Count + 1);
    }

    // The cellon is destroyed either way, so a failed roll leaves the jewel untouched.
    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx) { }

    protected override void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx)
    {
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Target!.ItemInstanceId);
    }

    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        var rolled = ((CellonRollData)ctx.ExtraData!).Rolled;
        if (rolled is not null)
        {
            await equipmentOptionDao.TryInsertOrUpdateAsync(rolled);
        }
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        var cellon = ctx.Target!;
        yield return (cellon.ItemInstance!.Amount > 0 ? cellon : null)
            .GeneratePocketChange((PocketType)cellon.Type, cellon.Slot);
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }

    // Only an upgrade material carries a tier in EffectValue, and only a cellon among them.
    private static bool TryResolve(ClientSession session, UpgradePacket packet,
        out InventoryItemInstance jewelSlot, out InventoryItemInstance cellonSlot)
    {
        jewelSlot = null!;
        cellonSlot = null!;
        if (packet.CellonInventoryType is null || packet.CellonSlot is null)
        {
            return false;
        }

        var jewel = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        var cellon = session.Character.InventoryService
            .LoadBySlotAndType(packet.CellonSlot.Value, (NoscorePocketType)packet.CellonInventoryType.Value);
        if (jewel?.ItemInstance is not WearableInstance { Item.MaxCellon: > 0 }
            || cellon?.ItemInstance?.Item is not { ItemType: ItemType.Upgrade, EffectValue: > 0 } cellonItem
            || cellonItem.EffectValue >= OptionsByCellonLevel.Length)
        {
            return false;
        }

        jewelSlot = jewel;
        cellonSlot = cellon;
        return true;
    }

    private HashSet<byte> LoadOptionTypes(InventoryItemInstance jewelSlot)
    {
        var jewelId = jewelSlot.ItemInstanceId;
        return equipmentOptionDao.Where(o => o.WearableInstanceId == jewelId)?
            .Select(o => o.Type).ToHashSet() ?? new HashSet<byte>();
    }

    private static IPacket[] Reject(Game18NConstString message) =>
        new IPacket[]
        {
            new MsgiPacket { Type = MessageType.Default, Message = message },
            new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow },
        };

    private sealed record CellonOption(CellonType Type, int Minimum, int Maximum);

    private sealed class CellonRollData(int level, HashSet<byte> taken)
    {
        public int Level { get; } = level;

        public HashSet<byte> Taken { get; } = taken;

        public CellonOption? Option { get; set; }

        public EquipmentOptionDto? Rolled { get; set; }
    }
}
