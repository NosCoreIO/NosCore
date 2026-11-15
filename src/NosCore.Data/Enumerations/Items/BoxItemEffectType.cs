//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Items
{
    // What the effect number means on an item of ItemType Box, where it does not match ItemEffectType.
    public enum BoxItemEffectType : ushort
    {
        // An empty bead stores the selected NosMate (sub type 1 a partner) and gives it back later.
        NosMateHolder = 0,

        // The NosMate whose monster is the effect value comes out of the bead; sub type 1 holds a partner.
        NosMateBead = 1
    }
}
