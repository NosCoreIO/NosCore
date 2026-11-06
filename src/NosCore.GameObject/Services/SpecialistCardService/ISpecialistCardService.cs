//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.SpecialistPointService;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.SpecialistCardService;

public interface ISpecialistCardService
{
    int GetFreePoints(SpecialistInstance specialist);

    // False, leaving the card untouched, when a value is negative or the card lacks the points.
    bool TrySpendPoints(SpecialistInstance specialist, short attack, short defence, short element, short hpMp);

    // False when nothing has been spent yet.
    bool TryResetPoints(SpecialistInstance specialist);

    SpecialistPointBonus GetBonus(SpecialistInstance specialist);
}
