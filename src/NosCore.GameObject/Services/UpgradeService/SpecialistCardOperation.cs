//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Core.I18N;
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

// An operation on the specialist card in the slot the packet names: refused with a message
// while the card cannot take it, and refreshing only that card afterwards.
public abstract class SpecialistCardOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const sbyte DestroyedSoulRare = -2;

    protected abstract Game18NConstString? Rejection(ClientSession session, SpecialistInstance card);

    protected override IReadOnlyList<IPacket>? TryReject(ClientSession session, UpgradePacket packet) =>
        LoadCard(session, packet)?.ItemInstance is SpecialistInstance card && Rejection(session, card) is { } rejection
            ? new IPacket[] { session.Character.GenerateSayi(SayColorType.Yellow, rejection) }
            : null;

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return RefreshSource(ctx);
    }

    protected static InventoryItemInstance? LoadCard(ClientSession session, UpgradePacket packet)
    {
        var source = LoadSource(session, packet);
        return source?.ItemInstance is SpecialistInstance { Item.EquipmentSlot: EquipmentType.Sp } ? source : null;
    }
}
