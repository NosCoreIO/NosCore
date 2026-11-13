//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.Parser.Parsers
{
    // The data files store an effect's first value as value * 4 + flags, signed. The flags are the
    // two low bits of the two's complement form, so a negative value cannot be split by dividing
    // its absolute value.
    public readonly record struct EffectValue(int Value, bool IsLevelScaled, bool IsLevelDivided)
    {
        public static EffectValue Decode(int raw)
        {
            var flags = raw & 3;
            return new EffectValue(Math.Abs(raw >> 2), flags != 0, flags == 2);
        }
    }
}
