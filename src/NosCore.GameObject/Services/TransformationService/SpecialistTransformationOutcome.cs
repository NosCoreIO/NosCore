//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.TransformationService;

public enum SpecialistTransformationOutcome
{
    Ignored,
    NoSpecialistCardEquipped,
    InVehicle,
    NoPoints,
    OnCooldown,
    Handled,
}
