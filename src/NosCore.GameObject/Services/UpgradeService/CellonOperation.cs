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

namespace NosCore.GameObject.Services.UpgradeService;

// Cellons add a permanent stat option to a piece of jewelry instead of raising its upgrade
// level. The cellon carries the option tier in Item.EffectValue, while the jewel caps both how
// many options it can hold (Item.MaxCellon) and how strong they may be (Item.MaxCellonLvl).
//
// The cellon is consumed on every attempt. Success odds fall as the jewel fills up, and a jewel
// that already carries every option its tier offers can no longer gain one.
[UsedImplicitly]
public sealed class CellonOperation(
    IRandomNumberSource random,
    IGameLanguageLocalizer localizer,
    IDao<EquipmentOptionDto, Guid> equipmentOptionDao)
    : UpgradeOperation(random, localizer)
{
    private static readonly long[] GoldCostByCellonLevel =
        { 0, 700, 1400, 3000, 5000, 10000, 20000, 32000, 58000, 95000, 134900 };

    private static readonly double[] SuccessRateByOptionCount =
        { 0.85, 0.75, 0.65, 0.50, 0.40, 0.30 };

    private static readonly CellonOption[][] OptionsByCellonLevel =
    {
        Array.Empty<CellonOption>(),
        new CellonOption[] { new(CellonType.Hp, 30, 100), new(CellonType.Mp, 50, 120), new(CellonType.HpRecovery, 5, 10), new(CellonType.MpRecovery, 8, 15) },
        new CellonOption[] { new(CellonType.Hp, 120, 200), new(CellonType.Mp, 150, 250), new(CellonType.HpRecovery, 14, 20), new(CellonType.MpRecovery, 16, 25) },
        new CellonOption[] { new(CellonType.Hp, 220, 330), new(CellonType.Mp, 280, 330), new(CellonType.HpRecovery, 22, 28), new(CellonType.MpRecovery, 28, 35) },
        new CellonOption[] { new(CellonType.Hp, 330, 400), new(CellonType.Mp, 350, 420), new(CellonType.HpRecovery, 30, 38), new(CellonType.MpRecovery, 38, 45) },
        new CellonOption[] { new(CellonType.Hp, 430, 550), new(CellonType.Mp, 450, 550), new(CellonType.HpRecovery, 40, 50), new(CellonType.MpRecovery, 50, 60) },
        new CellonOption[] { new(CellonType.Hp, 600, 750), new(CellonType.Mp, 600, 750), new(CellonType.HpRecovery, 55, 70), new(CellonType.MpRecovery, 65, 80), new(CellonType.MpConsumption, 1, 7), new(CellonType.CriticalDamageDecrease, 1, 7) },
        new CellonOption[] { new(CellonType.Hp, 800, 1000), new(CellonType.Mp, 800, 1000), new(CellonType.HpRecovery, 75, 90), new(CellonType.MpRecovery, 75, 90), new(CellonType.MpConsumption, 8, 12), new(CellonType.CriticalDamageDecrease, 11, 20) },
        new CellonOption[] { new(CellonType.Hp, 1000, 1300), new(CellonType.Mp, 1000, 1300), new(CellonType.HpRecovery, 100, 120), new(CellonType.MpRecovery, 100, 120), new(CellonType.MpConsumption, 13, 17), new(CellonType.CriticalDamageDecrease, 21, 35) },
        new CellonOption[] { new(CellonType.Hp, 1100, 1500), new(CellonType.Mp, 1100, 1500), new(CellonType.HpRecovery, 110, 135), new(CellonType.MpRecovery, 110, 135), new(CellonType.MpConsumption, 14, 21), new(CellonType.CriticalDamageDecrease, 22, 45) },
        new CellonOption[] { new(CellonType.Hp, 1200, 1700), new(CellonType.Mp, 1200, 1700), new(CellonType.HpRecovery, 120, 150), new(CellonType.MpRecovery, 120, 150), new(CellonType.MpConsumption, 15, 25), new(CellonType.CriticalDamageDecrease, 23, 55) },
    };

    public override UpgradePacketType Kind => UpgradePacketType.CellonItem;

    protected override Game18NConstString SuccessMessage => Game18NConstString.UpgradeSuccessful;

    protected override Game18NConstString FailureMessage => Game18NConstString.CellonDisapearedFailedUpgrade;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        if (packet.CellonInventoryType is null || packet.CellonSlot is null)
        {
            return null;
        }

        var jewelSlot = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        var cellonSlot = session.Character.InventoryService
            .LoadBySlotAndType(packet.CellonSlot.Value, (NoscorePocketType)packet.CellonInventoryType.Value);

        if (jewelSlot?.ItemInstance is not WearableInstance jewel || cellonSlot?.ItemInstance is null)
        {
            return null;
        }

        var level = cellonSlot.ItemInstance.Item.EffectValue;
        if (level <= 0 || level >= OptionsByCellonLevel.Length || level > jewel.Item.MaxCellonLvl)
        {
            return null;
        }

        var applied = jewel.Cellon ?? 0;
        if (applied >= jewel.Item.MaxCellon || applied >= SuccessRateByOptionCount.Length)
        {
            return null;
        }

        var jewelId = jewelSlot.ItemInstanceId;
        var taken = equipmentOptionDao.Where(o => o.WearableInstanceId == jewelId)?
            .Select(o => o.Type).ToHashSet() ?? new HashSet<byte>();
        var candidates = OptionsByCellonLevel[level]
            .Where(o => !taken.Contains((byte)o.Type))
            .ToArray();

        return new UpgradeContext(
            Source: jewelSlot,
            Target: cellonSlot,
            GoldCost: GoldCostByCellonLevel[level],
            MaterialCosts: Array.Empty<MaterialCost>(),
            ExtraData: new CellonRollData(level, applied, candidates));
    }

    // A jewel holding every option its tier offers has nothing left to roll, so the attempt
    // fails outright rather than reporting a success that adds nothing.
    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx) =>
        ((CellonRollData)ctx.ExtraData!).Candidates.Length == 0
            ? UpgradeOutcome.Failure
            : base.DetermineOutcome(roll, ctx);

    protected override double GetSuccessRate(UpgradeContext ctx) =>
        SuccessRateByOptionCount[((CellonRollData)ctx.ExtraData!).AppliedCount];

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var data = (CellonRollData)ctx.ExtraData!;
        var jewel = (WearableInstance)ctx.Source.ItemInstance!;
        var option = data.Candidates[Roll(data.Candidates.Length)];

        data.Rolled = new EquipmentOptionDto
        {
            Id = Guid.NewGuid(),
            WearableInstanceId = ctx.Source.ItemInstanceId,
            Level = (byte)data.Level,
            Type = (byte)option.Type,
            Value = option.Minimum + Roll(option.Maximum - option.Minimum + 1),
        };
        jewel.Cellon = (byte)(data.AppliedCount + 1);
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
        yield return ((InventoryItemInstance?)null).GeneratePocketChange(
            (PocketType)ctx.Target!.Type, ctx.Target.Slot);
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }

    private sealed record CellonOption(CellonType Type, int Minimum, int Maximum);

    private sealed class CellonRollData(int level, int appliedCount, CellonOption[] candidates)
    {
        public int Level { get; } = level;

        public int AppliedCount { get; } = appliedCount;

        public CellonOption[] Candidates { get; } = candidates;

        public EquipmentOptionDto? Rolled { get; set; }
    }
}
