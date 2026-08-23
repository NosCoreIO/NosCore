//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Character;

namespace NosCore.GameObject.Services.MateService
{
    public static class MateXpTable
    {
        private static readonly long[] RawCurve = BuildRawCurve();

        public const byte MaxDescribedLevel = 255;

        public static long RequiredXp(byte level, MateType mateType)
        {
            var index = level < 1 ? 0 : level - 1;
            if (index >= RawCurve.Length)
            {
                index = RawCurve.Length - 1;
            }

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
