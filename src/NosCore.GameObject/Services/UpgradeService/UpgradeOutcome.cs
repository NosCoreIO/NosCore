//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.UpgradeService;

// Outcome of a single upgrade attempt.
//   Success       → the operation's success path runs (Upgrade += 1, Rare += 1, etc.)
//   Failure       → the operation's failure path runs (item destroyed / degraded / unchanged)
//   Fixed         → the item is locked (IsFixed = true) and cannot be upgraded further;
//                   materials are consumed but no positive or negative state change to the item.
//                   Used by the equipment-upgrade flow per OpenNos's WearableInstance.UpgradeItem
//                   upfix table.
//   ProtectedSave → a failure roll was absorbed by a scroll. Materials are consumed but the
//                   item is saved in its original state (IsFixed is NOT set, unlike Fixed).
//                   OpenNos emits "SCROLL_PROTECT_USED" + "UPGRADE_FAILED_ITEM_SAVED" for this.
public enum UpgradeOutcome
{
    Success,
    Failure,
    Fixed,
    ProtectedSave,
}
