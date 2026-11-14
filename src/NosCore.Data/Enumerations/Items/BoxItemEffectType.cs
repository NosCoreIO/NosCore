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
        // The NosMate whose monster is the effect value comes out of the bead; sub type 1 holds a partner.
        NosMateBead = 1
    }
}
