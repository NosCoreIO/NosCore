//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class CharacterScreenTests
    {
        [TestMethod]
        public async Task ANewCharacterIsListedAndCanEnterTheWorld()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var name = "New" + player.Name;

            await client.SendAsync($"Char_NEW {name} 1 0 0 0");

            await client.ExpectAsync("clist_start");
            await client.ExpectAsync(p => p.StartsWith($"clist 1 {name} ", StringComparison.Ordinal), "the new character in slot 1");
            await client.ExpectAsync("clist_end");

            var entry = await client.EnterWorldAsync(1);
            Assert.IsTrue(entry.Any(p => p.StartsWith($"at {client.CharacterId} {WorldData.Town} ", StringComparison.Ordinal)),
                "a new character starts in the town");
        }

        [TestMethod]
        public async Task ACreationThatCannotBeStoredLeavesNothingBehind()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var name = "Half" + player.Name;

            // The character row is written first; this makes the write after it fail for this
            // character only.
            await using (var context = Harness.Database.CreateContext())
            {
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE FUNCTION refuse_half_created() RETURNS trigger AS $$
                    BEGIN
                        IF EXISTS (SELECT 1 FROM "Character" c WHERE c."CharacterId" = NEW."CharacterId" AND c."Name" LIKE 'Half%') THEN
                            RAISE EXCEPTION 'refused by the scenario';
                        END IF;
                        RETURN NEW;
                    END $$ LANGUAGE plpgsql;
                    CREATE TRIGGER refuse_half_created BEFORE INSERT ON "CharacterSkill"
                        FOR EACH ROW EXECUTE FUNCTION refuse_half_created();
                    """);
            }

            try
            {
                await client.SendAsync($"Char_NEW {name} 1 0 0 0");

                // 213 is the client's "Creation failed. Please try again."
                await client.ExpectAsync(p => p.StartsWith("infoi 213 ", StringComparison.Ordinal), "the creation-failed notice");
                await using var context = Harness.Database.CreateContext();
                Assert.IsFalse(await context.Set<Character>().AnyAsync(c => c.Name == name), "no character row is left");
            }
            finally
            {
                await using var context = Harness.Database.CreateContext();
                await context.Database.ExecuteSqlRawAsync("""
                    DROP TRIGGER refuse_half_created ON "CharacterSkill";
                    DROP FUNCTION refuse_half_created();
                    """);
            }
        }

        [TestMethod]
        public async Task ANameThatIsTakenIsRefused()
        {
            var taken = await Harness.AddPlayerAsync("Bob");
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var created = client.Mark();

            await client.SendAsync($"Char_NEW {taken.Name} 1 0 0 0");

            await client.ExpectAsync("infoi");
            Assert.IsTrue(await client.NoneSinceAsync(created, p => p.StartsWith("clist 1 ", StringComparison.Ordinal), TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task ADeletedCharacterLeavesTheList()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var deleted = client.Mark();

            await client.SendAsync($"Char_DEL {player.Slot} {WorldData.Password}");

            await client.ExpectAsync("clist_start");
            await client.ExpectAsync("clist_end");
            Assert.IsFalse(client.Since(deleted).Any(p => p.StartsWith("clist ", StringComparison.Ordinal)), "the slot is empty");
        }
    }
}
