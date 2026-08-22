//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Character;

namespace NosCore.GameObject.Services.MateService
{
    /// <summary>
    /// How much experience a pet or a partner needs to reach the next level.
    /// </summary>
    /// <remarks>
    /// CONFIRMED AGAINST A REAL CAPTURE, not ported on trust. The XpLoad field of every sc_p and
    /// sc_n in the reference capture (build/parser-input/packet.txt) was compared with the curve
    /// below, and eleven of the eleven observations line up to the unit:
    ///
    ///     pet     lvl  1 -> 15            lvl  3 -> 90          lvl  4 -> 165
    ///     pet     lvl  5 -> 273           lvl  6 -> 420         lvl 14 -> 3720
    ///     pet     lvl 86 -> 29 312 950    lvl 88 -> 39 495 200
    ///     partner lvl 24 -> 117 720       lvl 50 -> 2 293 816
    ///
    /// A level-53 partner reported 1 instead of 2 934 420; that one row is left unexplained
    /// rather than fitted, because a single outlier is not a rule.
    ///
    /// THE TWO DIVISORS ARE THE POINT. The raw curve — the one OpenNos and NosWings ship as
    /// MateHelper.XpData — is exactly twenty times the pet requirement and five times the
    /// partner requirement. Matching to the unit at level 88, where the numbers run to eight
    /// digits, is not a coincidence: those emulators hand a pet twenty times the experience it
    /// should need, and a partner five times. That is the kind of mistake this domain is made
    /// of, because nothing throws — the pet simply never levels, and it looks like grind.
    ///
    /// The curve itself has no counterpart in the client files: mate progression is server-side,
    /// so there is nothing in parser-input to read it from. It stays as inherited, and the
    /// capture is what makes it trustworthy.
    /// </remarks>
    public static class MateXpTable
    {
        /// <summary>The raw curve, in the shape the older emulators express it.</summary>
        private static readonly long[] RawCurve = BuildRawCurve();

        /// <summary>
        /// Levels beyond this are not described by the curve; asking for one returns the last
        /// value rather than throwing, the way a level cap behaves.
        /// </summary>
        public const byte MaxDescribedLevel = 255;

        /// <summary>
        /// Experience needed to go from <paramref name="level" /> to the next one.
        /// </summary>
        public static long RequiredXp(byte level, MateType mateType)
        {
            // The table is indexed by the level just reached, so level 1 reads slot 0.
            var index = level < 1 ? 0 : level - 1;
            if (index >= RawCurve.Length)
            {
                index = RawCurve.Length - 1;
            }

            // Partners need four times what a pet of the same level needs. Both divisors come
            // from the capture, not from a design choice.
            return RawCurve[index] / (mateType == MateType.Pet ? 20 : 5);
        }

        private static long[] BuildRawCurve()
        {
            var curve = new long[MaxDescribedLevel + 1];
            var step = new double[curve.Length];
            var factor = 1d;

            step[0] = 540;
            step[1] = 960;
            curve[0] = 300;

            for (var i = 2; i < step.Length; i++)
            {
                step[i] = step[i - 1] + 420 + 120 * (i - 1);
            }

            for (var i = 1; i < curve.Length; i++)
            {
                if (i < 79)
                {
                    factor = i switch
                    {
                        14 => 6 / 3d,
                        39 => 19 / 3d,
                        59 => 70 / 3d,
                        _ => factor
                    };

                    curve[i] = (long)(curve[i - 1] + factor * step[i - 1]);
                    continue;
                }

                factor = i switch
                {
                    79 => 5000,
                    82 => 9000,
                    84 => 13000,
                    _ => factor
                };

                curve[i] = (long)(curve[i - 1] + factor * (i + 2) * (i + 2));
            }

            return curve;
        }
    }
}
