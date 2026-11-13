//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Family
{
    // What a family quest counts, read from its category and sub-type columns in quest.dat.
    public enum FamilyQuestObjective : byte
    {
        FamilyLevel = 0,
        DailyMessage = 1,
        InstantCombat = 2
    }
}
