//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // Only the UK file is read: every language repeats the same numbers, but wrapped in
    // translated prose with its own separators and thousands marks, and CZ/PL are CP1250.
    internal static class ConstStringFile
    {
        public const string FileName = "conststring_UK.dat";

        public static async Task<Dictionary<int, string>> ReadAsync(string folder)
        {
            // Latin1 never throws on any byte, and callers only read digits out of the values.
            var content = await File.ReadAllTextAsync(Path.Combine(folder, FileName), Encoding.Latin1)
                .ConfigureAwait(false);

            var entries = new Dictionary<int, string>();
            foreach (var record in content.Split('\r', '\n'))
            {
                var separator = record.IndexOf('\v');
                if (separator <= 0)
                {
                    continue;
                }

                if (int.TryParse(record[..separator].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var key))
                {
                    entries[key] = record[(separator + 1)..];
                }
            }

            return entries;
        }
    }
}
