//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class RaidTests
    {
        private const short Cuby = 283;
        private const short CubyLever = 1135;
        private const short FieldX = 77;
        private const short FieldY = 167;
        private const short Glacerus = 2049;
        private const short GlacerusLever = 1000;
        private const short GrottoX = 13;
        private const short GrottoY = 39;
        private const short DenX = 32;
        private const short DenY = 48;

        // At most four cells on each axis, since a diagonal step of five is past what one walk may cover,
        // then the time an adventurer takes to cover them.
        private const int Stride = 4;
        private static readonly TimeSpan StrideTime = TimeSpan.FromMilliseconds(1300);

        private static Action<Character> RaidLevel => c => c.Level = WorldData.RaidLevel;

        private static Action<Character> GlacerusLevel => c => c.Level = WorldData.GlacerusLevel;

        private static Func<MsgiPacket, bool> Told(Game18NConstString message) => p => p.Message == message;

        private static async Task<TestClient> EnterAsync(Player player)
        {
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        private static Task<Player> AtTheGateAsync(string name, short x) =>
            Harness.AddPlayerAsync(name, WorldData.JellyEntrance, x, WorldData.RaidGateY + 1, RaidLevel);

        private static async Task OpenTeamAsync(TestClient leader)
        {
            var opened = leader.Mark();
            await leader.SendAsync(new UseItemPacket
            {
                VisualType = VisualType.Player, VisualId = leader.CharacterId, Type = PocketType.Main, Slot = 0, Mode = 0, Parameter = 0
            });
            await leader.ExpectSinceAsync(opened, Told(Game18NConstString.YouAreRaidLeader), "the leader told they lead the team");
        }

        private static async Task JoinAsync(TestClient leader, TestClient member)
        {
            var invited = member.Mark();
            await leader.SendAsync(new RdPacket { Type = 1, CharacterId = member.CharacterId });
            var invitation = await member.ExpectSinceAsync<Qnai2Packet>(invited, p => p.Question == Game18NConstString.AskJoinToTeam,
                "the invitation");
            var joined = member.Mark();
            await member.SendAsync(invitation.YesPacket ?? throw new AssertFailedException("the invitation carries no answer"));
            await member.ExpectSinceAsync(joined, Told(Game18NConstString.JoinedToTeam), "the member told they joined");
        }

        private static async Task WalkToAsync(TestClient client, short fromX, short fromY, short x, short y)
        {
            while (fromX != x || fromY != y)
            {
                fromX = (short)(fromX + Math.Clamp(x - fromX, -Stride, Stride));
                fromY = (short)(fromY + Math.Clamp(y - fromY, -Stride, Stride));
                await client.WalkAsync(fromX, fromY);
                await Task.Delay(StrideTime);
            }
        }

        [TestMethod]
        public async Task ARaidSealOpensATeamOthersJoinOnInvitation()
        {
            var leaderPlayer = await AtTheGateAsync("Lead", WorldData.RaidGateX);
            await Harness.GiveAsync(leaderPlayer, WorldData.RaidSeal, 1, pocket: NoscorePocketType.Main);
            await using var leader = await EnterAsync(leaderPlayer);
            await using var bob = await EnterAsync(await AtTheGateAsync("Bob", 22));

            await OpenTeamAsync(leader);
            await JoinAsync(leader, bob);

            var started = leader.Mark();
            await leader.SendAsync(new MkRaidPacket());
            await leader.ExpectSinceAsync(started, Told(Game18NConstString.TooFewRaidMember), "the leader told the team is too small");
            Assert.IsTrue(await leader.NoneSinceAsync<AtPacket>(started, null, TimeSpan.FromSeconds(1)), "the team stays at the gate");
        }

        private static async Task<List<TestClient>> StartTeamAsync(List<Player> players, List<TestClient> team)
        {
            foreach (var player in players)
            {
                team.Add(await EnterAsync(player));
            }

            await OpenTeamAsync(team[0]);
            foreach (var member in team.Skip(1))
            {
                await JoinAsync(team[0], member);
            }

            return team;
        }

        private static int CountSince(TestClient client, int mark, VisualType type, short vNum) =>
            client.Since<InPacket>(mark).Count(p => p.VisualType == type && p.VNum == vNum.ToString());

        [TestMethod]
        public async Task TheCubyRaidStartsInTheFieldWithTheSquareShutBehindItsObjectives()
        {
            var players = new List<Player>();
            for (var i = 0; i < WorldData.RaidTeamMinimum; i++)
            {
                players.Add(await AtTheGateAsync("Cuber", (short)(WorldData.RaidGateX + 2 * i)));
            }

            await Harness.GiveAsync(players[0], WorldData.RaidSeal, 1, pocket: NoscorePocketType.Main);
            var team = new List<TestClient>();
            try
            {
                await StartTeamAsync(players, team);
                var leader = team[0];
                var started = leader.Mark();
                await leader.SendAsync(new MkRaidPacket());
                await leader.ExpectSinceAsync<AtPacket>(started, p => p.MapId == WorldData.JellyField
                    && p.PositionX == FieldX && p.PositionY == FieldY, "the leader in the field");
                await leader.ExpectSinceAsync<GpPacket>(started, p => p.SourceX == 154 && p.SourceY == 11
                    && p.PortalType == PortalType.Closed, "the shut way to the square");

                var waited = System.Diagnostics.Stopwatch.StartNew();
                while (CountSince(leader, started, VisualType.Monster, Cuby) < 11 && waited.Elapsed < TestClient.DefaultTimeout)
                {
                    await Task.Delay(100);
                }

                Assert.AreEqual(11, CountSince(leader, started, VisualType.Monster, Cuby), "the eleven Cubies in the field");
                Assert.AreEqual(3, CountSince(leader, started, VisualType.Object, CubyLever), "the three levers that count");
            }
            finally
            {
                foreach (var raider in team)
                {
                    await raider.DisposeAsync();
                }
            }
        }

        [TestMethod]
        public async Task ATeamThatBeatsTheBossIsRewardedAndAdvancesItsFamilyMission()
        {
            var players = new List<Player>();
            for (var i = 0; i < WorldData.RaidTeamMinimum; i++)
            {
                players.Add(await Harness.AddPlayerAsync("Raider", WorldData.GlacerusEntrance, (short)(WorldData.GlacerusGateX + 2 * i),
                    WorldData.GlacerusGateY + 1, GlacerusLevel));
            }

            await Harness.GiveAsync(players[0], WorldData.GlacerusSeal, 1, pocket: NoscorePocketType.Main);
            var family = await Harness.AddFamilyAsync(players[0], players.Skip(1).ToArray());
            var team = new List<TestClient>();
            try
            {
                await StartTeamAsync(players, team);
                var leader = team[0];
                var started = leader.Mark();
                await leader.SendAsync(new MkRaidPacket());
                await leader.ExpectSinceAsync<AtPacket>(started, p => p.MapId == WorldData.GlacerusGrotto
                    && p.PositionX == GrottoX && p.PositionY == GrottoY, "the leader in the grotto");
                var lever = await leader.ExpectSinceAsync<InPacket>(started,
                    p => p.VisualType == VisualType.Object && p.VNum == GlacerusLever.ToString(), "the lever");

                await WalkToAsync(leader, GrottoX, GrottoY, 13, 23);
                var pulled = leader.Mark();
                await leader.SendAsync(new GetPacket { PickerType = VisualType.Player, PickerId = leader.CharacterId, VisualId = lever.VisualId });
                await leader.ExpectSinceAsync<DelayPacket>(pulled, null, "the lever's bar");
                await Task.Delay(2100);
                await leader.SendAsync(new GitPacket { ButtonId = (int)lever.VisualId });
                await leader.ExpectSinceAsync<GpPacket>(pulled, p => p.PortalType == PortalType.Open && !p.IsDisabled, "the way to the den open");

                await WalkToAsync(leader, 13, 23, 13, 7);
                var crossed = leader.Mark();
                await leader.SendAsync(new PreqPacket());
                await leader.ExpectSinceAsync<AtPacket>(crossed, p => p.MapId == WorldData.GlacerusDen, "the leader in the den");
                var boss = await leader.ExpectSinceAsync<InPacket>(crossed,
                    p => p.VisualType == VisualType.Monster && p.VNum == Glacerus.ToString(), "the boss in the den");

                await WalkToAsync(leader, DenX, DenY, 22, 14);
                var won = leader.Mark();
                await Steps.KillAsync(leader, boss.VisualId);

                foreach (var raider in team)
                {
                    await raider.ExpectSinceAsync<IvnPacket>(raider == leader ? won : 0,
                        p => p.Type == PocketType.Etc && p.IvnSubPackets?.Any(s => s.VNum == WorldData.GlacerusReward) == true,
                        "the raid's reward in the bag");
                }

                Assert.AreEqual(WorldData.RaidTeamMinimum, await MissionProgressAsync(family.FamilyId),
                    "every member counts once towards the family's raid mission");
            }
            finally
            {
                foreach (var raider in team)
                {
                    await raider.DisposeAsync();
                }
            }
        }

        private static async Task<int> MissionProgressAsync(long familyId)
        {
            var waited = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                await using (var context = Harness.Database.CreateContext())
                {
                    var progress = await context.Set<FamilyQuestProgress>().AsNoTracking()
                        .Where(p => p.FamilyId == familyId && p.QuestId == WorldData.RaidMissionId)
                        .Select(p => (int?)p.Progress).SingleOrDefaultAsync();
                    if (progress >= WorldData.RaidTeamMinimum || waited.Elapsed > TimeSpan.FromSeconds(10))
                    {
                        return progress ?? 0;
                    }
                }

                await Task.Delay(250);
            }
        }
    }
}
