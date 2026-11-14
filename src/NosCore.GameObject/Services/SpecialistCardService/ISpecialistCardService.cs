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

    // The boost adds SL levels on top of the card's own, as weapon shells do, up to the card's cap.
    SpecialistPointBonus GetBonus(SpecialistInstance specialist, SpecialistLevelBoost boost = default);
}

public readonly record struct SpecialistLevelBoost(int Attack, int Defence, int Element, int HpMp);
