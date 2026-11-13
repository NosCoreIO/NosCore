//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Items
{
    // What a holder box keeps, read from its effect value. Both specialist values take any card;
    // the item data alone does not say how the two holders differ.
    public enum HolderEffectValue : short
    {
        SpecialistCard = 1,
        GoldenSpecialistCard = 2,
        Fairy = 3
    }
}
