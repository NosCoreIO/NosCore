//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // The client lists what many chests hold only in their English description, one "<amount>x <item name>"
    // per line. A chest that says it gives one of them becomes a box that rolls one line, each line as likely;
    // a pack that says it holds the whole list becomes an item pack that gives every line.
    public partial class BoxContentParser(IDao<ItemDto, short> itemDao,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao, ILogger<BoxContentParser> logger)
    {
        private const string ItemFile = "Item.dat";
        private const string EnglishItemText = "_code_uk_Item.txt";

        [GeneratedRegex(@"^(\d+)x (.+)$")]
        private static partial Regex ContentLine();

        [GeneratedRegex(@"\bone of (the following|these)\b", RegexOptions.IgnoreCase)]
        private static partial Regex GivesOneOf();

        [GeneratedRegex(@"\b(contains|consists of|includes|receive the following items?)\b|\[(Contents|Items)\]", RegexOptions.IgnoreCase)]
        private static partial Regex GivesWholeList();

        // Wording that makes what comes out depend on luck or on the character, so the list is not what every opener gets.
        [GeneratedRegex(@"\b(random|one of|certain amount|available|a pair of|variety|gender)\b", RegexOptions.IgnoreCase)]
        private static partial Regex GivesSomeOf();

        public async Task InsertBoxContentsAsync(string folder)
        {
            var text = ReadText(Path.Combine(folder, EnglishItemText));
            var keys = ReadNameAndDescriptionKeys(Path.Combine(folder, ItemFile));
            var items = itemDao.LoadAll().ToDictionary(i => i.VNum);

            // A name shared by several items (a limited copy, a mall copy) means the oldest one.
            var vnumByName = keys
                .Where(k => text.ContainsKey(k.Value.Name))
                .GroupBy(k => text[k.Value.Name])
                .ToDictionary(g => g.Key, g => g.Min(k => k.Key));

            var rolls = new List<RollGeneratedItemDto>();
            var boxes = new List<ItemDto>();
            foreach (var (vnum, key) in keys)
            {
                if (!items.TryGetValue(vnum, out var box) || box.Effect is not (ItemEffectType.NoEffect or ItemEffectType.BoxEffect)
                    || key.Description == null || !text.TryGetValue(key.Description, out var description))
                {
                    continue;
                }

                var lines = description.Split("[n]");
                var intro = string.Join(' ', lines.Where(l => !ContentLine().IsMatch(l)));
                var contents = lines.Select(l => ContentLine().Match(l)).Where(m => m.Success)
                    .Select(m => (Amount: int.Parse(m.Groups[1].Value), Name: m.Groups[2].Value.Trim()))
                    .ToList();
                var givesOne = GivesOneOf().IsMatch(intro);
                var givesAll = !givesOne && GivesWholeList().IsMatch(intro) && !GivesSomeOf().IsMatch(intro);
                if (contents.Count == 0 || !(givesOne || givesAll)
                    || contents.Any(c => !vnumByName.ContainsKey(c.Name) || c.Amount > byte.MaxValue))
                {
                    continue;
                }

                box.Effect = givesOne ? ItemEffectType.BoxEffect : ItemEffectType.ItemPack;
                boxes.Add(box);
                rolls.AddRange(contents.Select(c => new RollGeneratedItemDto
                {
                    OriginalItemVNum = vnum,
                    ItemGeneratedVNum = vnumByName[c.Name],
                    ItemGeneratedAmount = (byte)c.Amount,
                    Probability = 1,
                }));
            }

            for (var i = 0; i < rolls.Count; i++)
            {
                rolls[i].RollGeneratedItemId = (short)(i + 1);
            }

            await rollGeneratedItemDao.TryDeleteAsync(rollGeneratedItemDao.LoadAll().Select(r => r.RollGeneratedItemId).ToList());
            await itemDao.TryInsertOrUpdateAsync(boxes);
            await rollGeneratedItemDao.TryInsertOrUpdateAsync(rolls);
            logger.LogInformation("{Boxes} boxes and packs take their {Rolls} items from their description", boxes.Count, rolls.Count);
        }

        private static Dictionary<string, string> ReadText(string file)
        {
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

        private static Dictionary<short, (string Name, string? Description)> ReadNameAndDescriptionKeys(string file)
        {
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
