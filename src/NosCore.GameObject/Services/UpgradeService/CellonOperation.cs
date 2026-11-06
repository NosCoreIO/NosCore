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
using NosCore.Algorithm.CellonService;
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
    IDao<EquipmentOptionDto, Guid> equipmentOptionDao,
    ICellonService cellonService)
    : UpgradeOperation(random, localizer)
{
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

        var level = (byte)cellonSlot.ItemInstance!.Item.EffectValue;
        return new UpgradeContext(
            Source: jewelSlot,
            Target: cellonSlot,
            GoldCost: cellonService.GetPrice(level),
            MaterialCosts: Array.Empty<MaterialCost>(),
            ExtraData: new CellonRollData(level, cellonService.GetOptions(level), LoadOptionTypes(jewelSlot)));
    }

    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var data = (CellonRollData)ctx.ExtraData!;
        var options = data.Options;
        data.Option = options[Math.Min((int)(roll * options.Count), options.Count - 1)];
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
            Level = data.Level,
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
            ((WearableInstance)ctx.Source.ItemInstance!).EquipmentOptions?.Add(rolled);
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
    private bool TryResolve(ClientSession session, UpgradePacket packet,
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
            || cellon?.ItemInstance?.Item is not { ItemType: ItemType.Upgrade, EffectValue: > 0 and <= byte.MaxValue } cellonItem
            || cellonService.GetOptions((byte)cellonItem.EffectValue).Count == 0)
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

    private sealed class CellonRollData(byte level, IReadOnlyList<CellonOption> options, HashSet<byte> taken)
    {
        public byte Level { get; } = level;

        public IReadOnlyList<CellonOption> Options { get; } = options;

        public HashSet<byte> Taken { get; } = taken;

        public CellonOption? Option { get; set; }

        public EquipmentOptionDto? Rolled { get; set; }
    }
}
