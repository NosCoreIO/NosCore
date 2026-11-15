//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Production;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.I18N;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class PacketTraceTests
    {
        private static readonly IPacket[] Session =
        [
            Captured.Arrival(1, 79, 108, music: 1),
            Captured.Npc(320, 1897, 50, 60, dialog: 5),
            Captured.Talk(NrunRunnerType.OpenProduction, 17, 1897),
            Captured.Makes(1002, 1003),
            Captured.Materials(10, (2029, 10)),
        ];

        private string _file = string.Empty;

        [TestInitialize]
        public void Setup() => _file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        [TestCleanup]
        public void Cleanup() => File.Delete(_file);

        [TestMethod]
        public async Task ACaptureIsReadBackInOrderAsTheServerAndTheClientSentItAsync()
        {
            var serializer = new Serializer(Session.Select(p => p.GetType()).Distinct()
                .Append(typeof(InNonPlayerSubPacket)).Append(typeof(InAliveSubPacket)).Append(typeof(MlistMaterialSubPacket)));
            await File.WriteAllLinesAsync(_file, Session.Select(serializer.Serialize));

            var read = await new PacketTrace(new Mock<ILogger<PacketTrace>>().Object,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object).ReadAsync(_file);

            CollectionAssert.AreEqual(Session.Select(p => p.GetType()).ToList(), read.Select(p => p.GetType()).ToList());
            Assert.AreEqual((short)1, ((AtPacket)read[0]).MapId);
            Assert.AreEqual((short)5, ((InPacket)read[1]).InNonPlayerSubPacket?.Dialog);
            Assert.AreEqual(1897L, ((NrunPacket)read[2]).VisualId);
            CollectionAssert.AreEqual(new short[] { 1002, 1003 }, ((MlistPacket)read[3]).ItemVNums);
            Assert.AreEqual((short)2029, ((MlistMaterialsPacket)read[4]).Materials.Single().VNum);
        }

        [TestMethod]
        public async Task ACaptureLogIsReadSessionBySessionAsync()
        {
            var serializer = new Serializer(Session.Select(p => p.GetType()).Distinct()
                .Append(typeof(InNonPlayerSubPacket)).Append(typeof(InAliveSubPacket)).Append(typeof(MlistMaterialSubPacket)));
            var lines = Session.Select(serializer.Serialize).ToList();
            await File.WriteAllLinesAsync(_file,
            [
                "=== session started 2026-11-15 20:43:29 ===",
                $"[20:44:13.633] [World] [Server] {lines[0]}",
                $"[20:44:13.656] [World] [Server] {lines[1]}",
                $"[20:44:13.700] [Login] [Server] {lines[3]}",
                "=== session started 2026-11-16 09:00:00 ===",
                $"[09:00:01.000] [World] [Client] {lines[2]}",
            ]);

            var sessions = await new PacketTrace(new Mock<ILogger<PacketTrace>>().Object,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object).ReadCaptureLogAsync(_file);

            CollectionAssert.AreEqual(new[] { new DateTime(2026, 11, 15, 20, 43, 29), new DateTime(2026, 11, 16, 9, 0, 0) },
                sessions.Select(s => s.Start).ToList());
            CollectionAssert.AreEqual(new[] { typeof(AtPacket), typeof(InPacket) }, sessions[0].Packets.Select(p => p.GetType()).ToList());
            Assert.AreEqual(1897L, ((NrunPacket)sessions[1].Packets.Single()).VisualId);
        }

        [TestMethod]
        public async Task AFileWithoutASessionHeaderIsNoCaptureLogAsync()
        {
            var arrival = new Serializer([typeof(AtPacket)]).Serialize(Session[0]);
            await File.WriteAllLinesAsync(_file, ["BEGIN", $"[20:44:13.633] [World] [Server] {arrival}"]);

            var sessions = await new PacketTrace(new Mock<ILogger<PacketTrace>>().Object,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object).ReadCaptureLogAsync(_file);

            Assert.AreEqual(0, sessions.Count);
        }
    }
}
