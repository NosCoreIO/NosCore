//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Buff;

namespace NosCore.Parser.Tests
{
    // documentation/dat/BCard.dat.md is the record of what BCard.dat declares. These
    // check BCardEffect against it, the way LogLanguageTests checks the language keys
    // against the resources: one direction always, the incomplete one opt-in.
    [TestClass]
    public class BCardVocabularyTests
    {
        [TestMethod]
        public void EveryEffectIsInTheVocabulary()
        {
            var documented = ReadVocabulary()
                .Where(row => row.Effect!.Length > 0)
                .Select(row => row.Effect!)
                .ToHashSet(StringComparer.Ordinal);

            var result = string.Join(Environment.NewLine, Enum.GetValues<BCardEffect>()
                .Where(effect => !documented.Contains(effect.ToString()))
                .Select(effect => $"{effect} = {(short)effect} is not in BCard.dat.md"));

            if (result.Length != 0)
            {
                Assert.Fail(result);
            }
        }

        [TestMethod]
        public void EveryNamedRowMatchesItsEffect()
        {
            var result = string.Join(Environment.NewLine, ReadVocabulary()
                .Where(row => row.Effect!.Length > 0)
                .Where(row => !Enum.TryParse<BCardEffect>(row.Effect, out var effect) || (short)effect != row.Key)
                .Select(row => $"{row.Effect} should be {row.Key} (type {row.Type}, subtype {row.SubType})"));

            if (result.Length != 0)
            {
                Assert.Fail(result);
            }
        }

        // Opt-in: 231 declared effects have no name yet. Mirrors CheckEveryLanguageValueSet,
        // which is optional for the same reason - the vocabulary is not finished.
        [TestCategory("OPTIONAL-TEST")]
        [TestMethod]
        public void EveryDeclaredEffectIsNamed()
        {
            var result = string.Join(Environment.NewLine, ReadVocabulary()
                .Where(row => row.Declared && row.Effect!.Length == 0)
                .Select(row => $"type {row.Type} subtype {row.SubType} has no BCardEffect member: {row.Text}"));

            if (result.Length != 0)
            {
                Assert.Fail(result);
            }
        }

        private static IEnumerable<VocabularyRow> ReadVocabulary()
        {
            var path = DocumentationPaths.For("BCard.dat.md");
            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith("| ", StringComparison.Ordinal))
                {
                    continue;
                }

                var cells = line.Split('|', StringSplitOptions.None);
                if (cells.Length < 6 || !byte.TryParse(cells[1].Trim(), out var type))
                {
                    continue;
                }

                yield return new VocabularyRow(type, byte.Parse(cells[2].Trim()),
                    cells[3].Trim() == "yes", cells[4].Trim(), cells[5].Trim());
            }
        }

        private sealed record VocabularyRow(byte Type, byte SubType, bool Declared, string? Effect, string Text)
        {
            public short Key => (short)(Type * 100 + SubType);
        }
    }
}
