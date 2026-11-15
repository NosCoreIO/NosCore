//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Shops
{
    // A service id from shoptype.dat: one tab of an NPC's shop window.
    public enum NpcService : byte
    {
        Upgrade = 4,
        Rarify = 6,
        AccessoryOption = 10,
        ResistanceSum = 12,
        SpecialistCardUpgrade = 20,
        FamilyWarehouse = 41,
        SpecialistPerfection = 45
    }
}
