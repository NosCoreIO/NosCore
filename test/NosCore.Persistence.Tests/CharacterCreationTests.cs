//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.Persistence.Tests
{
    [TestClass]
    public class CharacterCreationTests
    {
        private const string Name = "Persisted";

        private PostgresDatabase _database;
        private TestWorld World;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = await PostgresDatabase.CreateAsync();
            World = await TestWorld.CreateAsync(_database);
            Session = await World.CreateSessionAsync(new List<IPacketHandler> { World.Create<CharNewPacketHandler>() });
            Session.ClearPlayerEntity();
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_database is { } database)
            {
                await database.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task ACreationTheDatabaseRefusesLeavesNoCharacter()
        {
            await new Spec("A creation whose starter skills are not in the database leaves no character without its skills")
                .WhenAsync(CreatingACharacter)
                .ThenAsync(NoCharacterIsStored)
                .And(ThePlayerIsToldTheCreationFailed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACreatedCharacterIsStoredWithItsSkillsAndQuickSlots()
        {
            await new Spec("A created character is stored with its skills and quick slots")
                .GivenAsync(TheStarterSkillsAreInTheDatabase)
                .WhenAsync(CreatingACharacter)
                .ThenAsync(TheCharacterIsStoredWithItsSkillsAndQuickSlots)
                .ExecuteAsync();
        }

        private async Task TheStarterSkillsAreInTheDatabase()
        {
            await using var context = _database.CreateContext();
            context.Set<Skill>().AddRange(new short[] { 200, 201, 209 }.Select(vnum => new Skill { SkillVNum = vnum, Name = "skill" + vnum }));
            await context.SaveChangesAsync();
        }

        private Task CreatingACharacter() => Session.HandlePacketsAsync(new[] { new CharNewPacket { Name = Name } });

        private async Task NoCharacterIsStored()
        {
            await using var context = _database.CreateContext();
            Assert.IsFalse(await context.Set<Character>().AnyAsync(c => c.Name == Name));
            Assert.IsFalse(await context.Set<QuicklistEntry>().AnyAsync());
        }

        private void ThePlayerIsToldTheCreationFailed() =>
            Assert.AreEqual(Game18NConstString.CreationFailed, Session.LastPackets.OfType<InfoiPacket>().Single().Message);

        private async Task TheCharacterIsStoredWithItsSkillsAndQuickSlots()
        {
            await using var context = _database.CreateContext();
            var character = await context.Set<Character>().SingleAsync(c => c.Name == Name);
            CollectionAssert.AreEquivalent(new short[] { 200, 201, 209 }, await context.Set<CharacterSkill>()
                .Where(s => s.CharacterId == character.CharacterId).Select(s => s.SkillVNum).ToListAsync());
            Assert.AreEqual(4, await context.Set<QuicklistEntry>().CountAsync(q => q.CharacterId == character.CharacterId));
        }
    }
}
