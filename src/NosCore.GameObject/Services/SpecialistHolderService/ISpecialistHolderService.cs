//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.SpecialistHolderService;

public interface ISpecialistHolderService
{
    bool IsSpecialistHolder(IItemInstance? item);

    // A card whose soul was destroyed stays out of holders, so it cannot be traded away unrevived.
    bool CanStore(SpecialistInstance card);

    // Moves the card's progress into the empty holder; the caller removes the card itself.
    void Store(SpecialistInstance card, BoxInstance holder);

    // Moves a card from the equipment pocket into an empty holder there and removes the card.
    SpecialistStoreResult StoreFromEquipment(PlayerComponentBundle character, short cardSlot, short holderSlot);

    // A new card carrying the progress the holder kept; the holder is left unchanged.
    SpecialistInstance TakeOut(BoxInstance holder);
}
