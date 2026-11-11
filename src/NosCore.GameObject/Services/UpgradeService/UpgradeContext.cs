//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using NosCore.GameObject.Services.InventoryService;

namespace NosCore.GameObject.Services.UpgradeService;

// Bag of validated input + computed costs that the base UpgradeOperation skeleton passes
// between its hooks. Subclasses can stash extra context via ExtraData when needed.
public sealed record UpgradeContext(
    InventoryItemInstance Source,
    InventoryItemInstance? Target,
    long GoldCost,
    IReadOnlyList<MaterialCost> MaterialCosts,
    object? ExtraData = null)
{
    public InventoryItemInstance RequiredTarget =>
        Target ?? throw new InvalidOperationException("This upgrade has no target.");

    public T Data<T>() => ExtraData is T data
        ? data
        : throw new InvalidOperationException($"This upgrade carries no {typeof(T).Name}.");
}

public sealed record MaterialCost(short VNum, short Amount, bool OnSuccessOnly = false);
