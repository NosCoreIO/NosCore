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
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class CharacterScreenTests
    {
        private static CharNewPacket NewCharacter(string name) => new()
        {
            Name = name,
            Slot = 1,
            Gender = GenderType.Male,
            HairStyle = HairStyleType.HairStyleA,
            HairColor = HairColorType.DarkPurple,
        };

        [TestMethod]
        public async Task ANewCharacterIsListedAndCanEnterTheWorld()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var name = "New" + player.Name;

            await client.SendAsync(NewCharacter(name));

            await client.ExpectAsync<ClistStartPacket>();
            await client.ExpectAsync<ClistPacket>(p => p.Slot == 1 && p.Name == name, "the new character in slot 1");
            await client.ExpectAsync<ClistEndPacket>();

            var entry = await client.EnterWorldAsync(1);
            Assert.IsTrue(entry.OfType<AtPacket>().Any(p => p.CharacterId == client.CharacterId && p.MapId == WorldData.Town),
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
                await client.SendAsync(NewCharacter(name));

                await client.ExpectAsync<InfoiPacket>(p => p.Message == Game18NConstString.CreationFailed, "the creation-failed notice");
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

            await client.SendAsync(NewCharacter(taken.Name));

            await client.ExpectAsync<InfoiPacket>();
            Assert.IsTrue(await client.NoneSinceAsync<ClistPacket>(created, p => p.Slot == 1, TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task ADeletedCharacterLeavesTheList()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var deleted = client.Mark();

            await client.SendAsync(new CharacterDeletePacket { Slot = player.Slot, Password = WorldData.Password });

            await client.ExpectAsync<ClistStartPacket>();
            await client.ExpectAsync<ClistEndPacket>();
            Assert.IsFalse(client.Since<ClistPacket>(deleted).Any(), "the slot is empty");
        }
    }
}
