//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Algorithm.SpPerfectionService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// Every attempt costs gold and perfection stones. A success raises the card's perfection by one
// and adds points to one of its sixteen-sided roll's stats: attack, defence, element and HP/MP
// each take three faces, the four resistances one each.
[UsedImplicitly]
public sealed class SpPerfectionOperation(
    IRandomNumberSource random,
    IGameLanguageLocalizer localizer,
    ISpPerfectionService spPerfectionService)
    : SpecialistCardOperation(random, localizer)
{
    public const short SmallRubyVNum = 2514;
    public const short SmallSapphireVNum = 2515;
    public const short SmallObsidianVNum = 2516;
    public const short SmallTopazVNum = 2517;
    public const short RubyVNum = 2518;
    public const short SapphireVNum = 2519;
    public const short ObsidianVNum = 2520;
    public const short TopazVNum = 2521;

    private const int StatFaces = 16;

    // The client reads the first argument as a message text and the second as a number.
    private const byte StatArgumentType = 16;

    public override UpgradePacketType Kind => UpgradePacketType.PerfectSp;

    protected override Game18NConstString SuccessMessage => Game18NConstString.PerfectingSpecialistSuccessful;

    protected override Game18NConstString FailureMessage => Game18NConstString.PerfectingSpecialistFailed;

    protected override Game18NConstString? Rejection(ClientSession session, SpecialistInstance card) =>
        card.Rare == DestroyedSoulRare
            ? Game18NConstString.CantUseBecauseSoulDestroyed
            : card.SpStoneUpgrade >= spPerfectionService.MaxPerfection
                ? Game18NConstString.MaximumUpgradeReached
                : null;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = LoadCard(session, packet);
        if (source?.ItemInstance is not SpecialistInstance card || StoneVNum(card.Item.Morph) is not { } stone)
        {
            return null;
        }

        var perfection = card.SpStoneUpgrade;
        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: spPerfectionService.GetGoldCost(perfection),
            MaterialCosts: new[] { new MaterialCost(stone, spPerfectionService.GetStoneCost(perfection)) },
            ExtraData: new PerfectionGain());
    }

    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx) =>
        roll * 100 < spPerfectionService.GetSuccessRate(((SpecialistInstance)ctx.Source.ItemInstance).SpStoneUpgrade)
            ? UpgradeOutcome.Success
            : UpgradeOutcome.Failure;

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var card = (SpecialistInstance)ctx.Source.ItemInstance;
        var gain = ctx.Data<PerfectionGain>();
        var minimum = spPerfectionService.GetMinimumGain(card.SpStoneUpgrade);
        gain.Points = (byte)(minimum + Roll(spPerfectionService.GetMaximumGain(card.SpStoneUpgrade) - minimum + 1));
        gain.Stat = Roll(StatFaces) switch
        {
            < 3 => Game18NConstString.Attack,
            < 6 => Game18NConstString.Defence,
            < 9 => Game18NConstString.Element,
            < 12 => Game18NConstString.HpMp,
            12 => Game18NConstString.FireResistance,
            13 => Game18NConstString.WaterResistance,
            14 => Game18NConstString.LightResistance,
            _ => Game18NConstString.ShadowResistance,
        };

        switch (gain.Stat)
        {
            case Game18NConstString.Attack:
                card.SpDamage += gain.Points;
                break;
            case Game18NConstString.Defence:
                card.SpDefence += gain.Points;
                break;
            case Game18NConstString.Element:
                card.SpElement += gain.Points;
                break;
            case Game18NConstString.HpMp:
                card.SpHp += gain.Points;
                break;
            case Game18NConstString.FireResistance:
                card.SpFire += gain.Points;
                break;
            case Game18NConstString.WaterResistance:
                card.SpWater += gain.Points;
                break;
            case Game18NConstString.LightResistance:
                card.SpLight += gain.Points;
                break;
            default:
                card.SpDark += gain.Points;
                break;
        }

        card.SpStoneUpgrade++;
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx) { }

    protected override int? OutcomeEffect(UpgradeOutcome outcome) => outcome == UpgradeOutcome.Success ? 3005 : null;

    // The chat line cannot carry the stat's name, so a success is only shown in the centre.
    protected override SayiPacket? BuildSay(ClientSession session, UpgradeContext ctx, UpgradeOutcome outcome,
        Game18NConstString message) =>
        outcome == UpgradeOutcome.Success ? null : base.BuildSay(session, ctx, outcome, message);

    protected override MsgiPacket BuildMsgi(UpgradeContext ctx, UpgradeOutcome outcome, Game18NConstString message)
    {
        var msgi = base.BuildMsgi(ctx, outcome, message);
        if (outcome == UpgradeOutcome.Success)
        {
            var gain = ctx.Data<PerfectionGain>();
            msgi.ArgumentType = StatArgumentType;
            msgi.Game18NArguments.Add((long)gain.Stat);
            msgi.Game18NArguments.Add((long)gain.Points);
        }

        return msgi;
    }

    private static short? StoneVNum(short morph) => morph switch
    {
        2 or 6 or 9 or 12 => SmallRubyVNum,
        3 or 4 or 14 => SmallSapphireVNum,
        5 or 11 or 15 => SmallObsidianVNum,
        7 or 8 or 10 or 13 => SmallTopazVNum,
        17 or 18 or 19 => RubyVNum,
        20 or 21 or 22 => SapphireVNum,
        23 or 24 or 25 => ObsidianVNum,
        26 or 27 or 28 => TopazVNum,
        _ => null,
    };

    private sealed class PerfectionGain
    {
        public byte Points { get; set; }

        public Game18NConstString Stat { get; set; }
    }
}
