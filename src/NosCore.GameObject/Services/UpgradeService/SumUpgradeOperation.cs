//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.SumService;
using NosCore.GameObject.Services.ItemGenerationService;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// Combines two boots or two gloves: on success the source gains the target's upgrade plus one
// and both resistances; on failure both are lost. Gold and sand are spent either way.
[UsedImplicitly]
public sealed class SumUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        IItemCatalog itemCatalog, ISumService sumService)
    : UpgradeOperation(random, localizer)
{
    public override UpgradePacketType Kind => UpgradePacketType.SumResistance;

    protected override Game18NConstString SuccessMessage => Game18NConstString.CombinationSuccessful;

    protected override Game18NConstString FailureMessage => Game18NConstString.CombinationFailed;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        if (packet.InventoryType2 is null || packet.Slot2 is null)
        {
            return null;
        }

        var source = LoadSource(session, packet);
        var target = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot2.Value, (NoscorePocketType)packet.InventoryType2.Value);

        if (source?.ItemInstance is not WearableInstance s || target?.ItemInstance is not WearableInstance t)
        {
            return null;
        }

        if (source.ItemInstanceId == target.ItemInstanceId)
        {
            return null;
        }

        if (s.Item.EquipmentSlot != t.Item.EquipmentSlot
            || (s.Item.EquipmentSlot != EquipmentType.Boots
                && s.Item.EquipmentSlot != EquipmentType.Gloves))
        {
            return null;
        }

        if (s.Upgrade + t.Upgrade > sumService.MaxLevel)
        {
            return null;
        }

        return new UpgradeContext(
            Source: source,
            Target: target,
            GoldCost: sumService.GetPrice(s.Upgrade),
            MaterialCosts: new[] { new MaterialCost(itemCatalog.GetMaterialVNum(MaterialType.RiverSand), (short)sumService.GetSandCost(s.Upgrade)) },
            ExtraData: s.Upgrade + t.Upgrade);
    }

    protected override double GetSuccessRate(UpgradeContext ctx) =>
        sumService.GetSuccessRate((byte)ctx.Data<int>()) / 100d;

    // The target contributes its base item resistance as well as what it has already accumulated.
    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var source = (WearableInstance)ctx.Source.ItemInstance;
        var target = (WearableInstance)ctx.RequiredTarget.ItemInstance;

        source.DarkResistance  = (short)((source.DarkResistance  ?? 0) + (target.DarkResistance  ?? 0) + target.Item.DarkResistance);
        source.LightResistance = (short)((source.LightResistance ?? 0) + (target.LightResistance ?? 0) + target.Item.LightResistance);
        source.FireResistance  = (short)((source.FireResistance  ?? 0) + (target.FireResistance  ?? 0) + target.Item.FireResistance);
        source.WaterResistance = (short)((source.WaterResistance ?? 0) + (target.WaterResistance ?? 0) + target.Item.WaterResistance);
        source.Upgrade = (byte)(source.Upgrade + target.Upgrade + 1);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Source.ItemInstanceId);
    }

    protected override void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx)
    {
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.RequiredTarget.ItemInstanceId);
    }

    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        playerPackets.Add(new GuriPacket
        {
            Type = GuriPacketType.AfterSumming,
            Argument = 1,
            SecondArgument = 0,
            EntityId = session.Character.CharacterId,
            Value = (uint)(outcome == UpgradeOutcome.Success ? 1324 : 1332),
        });
        await session.Character.MapInstance.SendPacketAsync(new GuriPacket
        {
            Type = GuriPacketType.CharacterAnimation,
            Argument = 1,
            SecondArgument = 0,
            EntityId = session.Character.CharacterId,
        });
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return ((InventoryItemInstance?)null).GeneratePocketChange(
            ctx.RequiredTarget.Type.ToPocketType(), ctx.RequiredTarget.Slot);
        if (outcome == UpgradeOutcome.Success)
        {
            yield return RefreshSource(ctx);
            var source = (WearableInstance)ctx.Source.ItemInstance;
            yield return new PdtiPacket
            {
                Type = PdtiPacketType.ResistanceFused,
                Vnum = source.ItemVNum,
                RecipeAmount = 1,
                Slot = 27,
                ItemUpgrade = source.Upgrade,
                Rare = 0,
            };
        }
        else
        {
            yield return ((InventoryItemInstance?)null).GeneratePocketChange(
                ctx.Source.Type.ToPocketType(), ctx.Source.Slot);
        }
    }
}
