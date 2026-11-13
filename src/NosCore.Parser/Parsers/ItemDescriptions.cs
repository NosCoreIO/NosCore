//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NosCore.Parser.Parsers
{
    // Item.dat names each item's name and description by key; the English text file holds the sentences.
    public static class ItemDescriptions
    {
        private const string ItemFile = "Item.dat";
        private const string EnglishItemText = "_code_uk_Item.txt";

        public static Dictionary<short, string> Read(string folder)
        {
            var text = ReadText(folder);
            return ReadNameAndDescriptionKeys(folder)
                .Where(k => k.Value.Description != null && text.ContainsKey(k.Value.Description))
                .ToDictionary(k => k.Key, k => text[k.Value.Description ?? ""]);
        }

        public static Dictionary<string, string> ReadText(string folder)
        {
            var file = Path.Combine(folder, EnglishItemText);
            var text = new Dictionary<string, string>();
            foreach (var line in File.ReadAllText(file, Encoding.Default).Split(["\r\n", "\r", "\n"], StringSplitOptions.None))
            {
                var columns = line.Split('\t');
                if (columns.Length > 1)
                {
                    text.TryAdd(columns[0], columns[1]);
                }
            }

            return text;
        }

        public static Dictionary<short, (string Name, string? Description)> ReadNameAndDescriptionKeys(string folder)
        {
            var file = Path.Combine(folder, ItemFile);
            var keys = new Dictionary<short, (string Name, string? Description)>();
            short? vnum = null;
            string? name = null;
            var descriptionFollows = false;
            foreach (var line in File.ReadAllText(file, Encoding.Default).Split(["\r\n", "\r", "\n"], StringSplitOptions.None))
            {
                var columns = line.Trim().Split('\t');
                if (descriptionFollows)
                {
                    descriptionFollows = false;
                    if (vnum is { } described && name != null)
                    {
                        keys[described] = (name, columns[0]);
                    }

                    continue;
                }

                switch (columns[0])
                {
                    case "VNUM" when columns.Length > 1:
                        vnum = short.Parse(columns[1]);
                        name = null;
                        break;
                    case "NAME" when columns.Length > 1 && vnum is { } named:
                        name = columns[1];
                        keys[named] = (name, null);
                        break;
                    case "LINEDESC":
                        descriptionFollows = true;
                        break;
                }
            }

            return keys;
        }
    }
}
