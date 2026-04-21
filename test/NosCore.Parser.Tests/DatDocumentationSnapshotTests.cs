//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Parser.Tests
{
    // Generates the per-parser Markdown from its FluentParserBuilder metadata and
    // writes it under <repo>/documentation/dat/. One method per parser so test
    // names identify which doc was regenerated.
    [TestClass]
    public class DatDocumentationSnapshotTests
    {
        [TestMethod] public void Item() => RegenerateFor(new ItemParser(
            Mock<ItemDto, short>(), Mock<BCardDto, short>(), Logger(), LogLang()).BuildParser("."));

        [TestMethod] public void Card() => RegenerateFor(new CardParser(
            Mock<CardDto, short>(), Mock<BCardDto, short>(), Logger(), LogLang()).BuildParser("."));

        [TestMethod] public void Skill() => RegenerateFor(new SkillParser(
            Mock<BCardDto, short>(), Mock<ComboDto, int>(), Mock<SkillDto, short>(), Logger(), LogLang()).BuildParser("."));

        [TestMethod] public void NpcMonster() => RegenerateFor(new NpcMonsterParser(
            Mock<SkillDto, short>(), Mock<BCardDto, short>(), Mock<DropDto, short>(),
            Mock<NpcMonsterSkillDto, long>(), Mock<NpcMonsterDto, short>(), Logger(), LogLang())
            .BuildParser("."));

        [TestMethod] public void Quest() => RegenerateFor(new QuestParser(
            Mock<QuestDto, short>(), Mock<QuestObjectiveDto, Guid>(),
            Mock<QuestRewardDto, short>(), Mock<QuestQuestRewardDto, Guid>(), Logger(), LogLang())
            .BuildParser("."));

        private static void RegenerateFor<T>(FluentParserBuilder<T> builder) where T : new()
        {
            var md = DatDocumentationGenerator.Generate(builder);
            var path = DocumentationPaths.For(builder.FileName + ".md");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, md);
        }

        private static IDao<TDto, TKey> Mock<TDto, TKey>() where TDto : class => new Mock<IDao<TDto, TKey>>().Object;
        private static ILogger Logger() => new Mock<ILogger>().Object;
        private static ILogLanguageLocalizer<LogLanguageKey> LogLang() => new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object;
    }

    internal static class DocumentationPaths
    {
        public static string For(string fileName)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "NosCore.sln")))
            {
                dir = dir.Parent;
            }
            if (dir == null)
            {
                throw new DirectoryNotFoundException("Could not locate repo root (NosCore.sln)");
            }
            return Path.Combine(dir.FullName, "documentation", "dat", fileName);
        }
    }
}
