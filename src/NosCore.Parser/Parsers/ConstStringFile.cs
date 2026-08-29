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
    // conststring is one long run of "<key>\v<value>\r" records holding the client's UI strings.
    // Only the UK file is read: the same numbers appear in all nine languages, but each wraps
    // them in translated prose with its own separators and thousands marks, and CZ/PL are CP1250
    // rather than CP1252.
    internal static class ConstStringFile
    {
        public const string FileName = "conststring_UK.dat";

        public static async Task<Dictionary<int, string>> ReadAsync(string folder)
        {
            // Latin1 round-trips every byte, so a client shipping a different codepage cannot make
            // the read throw. Callers only consume digits out of the values.
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
