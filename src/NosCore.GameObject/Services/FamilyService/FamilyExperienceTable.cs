//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.FamilyService
{
    /// <summary>
    /// How much experience a family needs to leave its current level.
    /// </summary>
    /// <remarks>
    /// INHERITED, AND THE ONE THING WE CAN CHECK SAYS IT IS WRONG. The table below is the one the
    /// older emulators ship. The capture contains a family at level 7, and its ginfo says the bar
    /// runs to 640 000:
    ///
    ///     ginfo -Nemesis- Yzigor 0 7 130000 640000 68 70 3 ...
    ///                              ^ level     ^ this
    ///
    /// The table says 1 900 000 for that level — nearly three times as much. One observation is
    /// not enough to rebuild nineteen rows, so the inherited numbers stay and the disagreement is
    /// written down instead of papered over. It is used only to draw the bar; nothing levels a
    /// family up yet, so being wrong costs a misdrawn bar and not lost progress.
    ///
    /// Closing it needs ginfo lines from families at several different levels — see Q16.
    /// </remarks>
    public static class FamilyExperienceTable
    {
        /// <summary>
        /// Experience to leave <paramref name="familyLevel" />, or a number the bar can never
        /// fill once past the last described level.
        /// </summary>
        public static uint RequiredExperience(byte familyLevel)
        {
            return familyLevel switch
            {
                1 => 100_000,
                2 => 250_000,
                3 => 370_000,
                4 => 560_000,
                5 => 840_000,
                6 => 1_260_000,
                7 => 1_900_000,
                8 => 2_850_000,
                9 => 3_570_000,
                10 => 3_830_000,
                11 => 4_150_000,
                12 => 4_750_000,
                13 => 5_500_000,
                14 => 6_500_000,
                15 => 7_000_000,
                16 => 8_500_000,
                17 => 9_500_000,
                18 => 10_000_000,
                19 => 17_000_000,
                // Past the last level the table describes. A full bar that never moves is the
                // honest picture of "there is nothing after this"; a zero would make the client
                // divide by it.
                _ => 999_999_999
            };
        }
    }
}
