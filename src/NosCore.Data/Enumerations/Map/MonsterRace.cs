//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Map
{
    // The monster's Race column. Its RaceType picks a subtype within the group, and the client
    // names each pair with conststring 307 + Race * 10 + RaceType.
    public enum MonsterRace : byte
    {
        // Plant, animal, monster, dragon, robot.
        LowLevel = 0,
        HighLevel = 1,
        // Kovolt, Bushtail, Catsy.
        Furry = 2,
        // Human, elf, half, demon, orc, draconian, mullan.
        People = 3,
        // Angel, devil, half angel.
        Celestial = 4,
        // Low-level undead, high-level undead, vampire.
        Undead = 5,
        // Low- and high-level spirit and ghost.
        Spirit = 6,
        // Machine, doll.
        Construct = 7,
        // Fixed trap, energy ball, cannon ball, Mini-Land structure.
        Object = 8,
        // Non-combat, fixed, portable, small.
        Other = 9,
    }

    // RaceType of the LowLevel and HighLevel groups.
    public enum MonsterRaceType : byte
    {
        Plant = 0,
        Animal = 1,
        Monster = 2,
        Dragon = 3,
        Robot = 4,
    }
}
