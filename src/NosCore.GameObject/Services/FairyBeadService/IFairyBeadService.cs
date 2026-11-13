//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.FairyBeadService;

// An empty fairy bead takes one fairy with its level; a full one gives it back.
public interface IFairyBeadService
{
    bool IsFairyBead(IItemInstance? item);

    // Moves the fairy at fairyPocket/fairySlot into the empty bead in the equipment pocket and removes it
    // from the bag. Null when the pair does not fit.
    FairyStored? Store(PlayerComponentBundle character, short beadSlot, NoscorePocketType fairyPocket, short fairySlot);

    // A new fairy carrying what the bead kept; the bead is left unchanged.
    WearableInstance TakeOut(BoxInstance bead);
}
