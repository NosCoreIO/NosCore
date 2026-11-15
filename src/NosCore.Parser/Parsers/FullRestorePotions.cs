//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // Potions that restore the whole bar. Item.dat gives them no Hp or Mp amount; the English
    // descriptions say "Restores 100%".
    public static class FullRestorePotions
    {
        public static readonly IReadOnlyDictionary<short, FullRestore> All = new Dictionary<short, FullRestore>
        {
            [1242] = new(Hp: true, Mp: false),
            [5582] = new(Hp: true, Mp: false),
            [1243] = new(Hp: false, Mp: true),
            [5583] = new(Hp: false, Mp: true),
            [1244] = new(Hp: true, Mp: true),
            [5584] = new(Hp: true, Mp: true),
            [9553] = new(Hp: true, Mp: false),
            [9942] = new(Hp: true, Mp: false),
            [13584] = new(Hp: true, Mp: false),
            [13585] = new(Hp: false, Mp: true),
            [13797] = new(Hp: true, Mp: true),
        };
    }

    public sealed record FullRestore(bool Hp, bool Mp);
}
