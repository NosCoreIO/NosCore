//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.SubscriberCacheService
{
    [TestClass]
    public class SubscriberCacheServiceTests
    {
        private const long FriendId = 7;
        private const long LeftSession = 100;
        private const long NewSession = 101;
        private TestWorld World;
        private Mock<IPubSubHub> PubSubHub;
        private GameObject.Services.SubscriberCacheService.SubscriberCacheService Cache;
        private List<Subscriber> Listed = new();
        private Subscriber? Found;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            PubSubHub = World.Freeze<Mock<IPubSubHub>>();
            PubSubHub.Setup(x => x.GetSubscribersAsync()).ReturnsAsync(() => Listed);
            Cache = World.Create<GameObject.Services.SubscriberCacheService.SubscriberCacheService>();
        }

        [TestMethod]
        public async Task ASessionThatLeftThisChannelIsNotFoundWhileTheMasterStillListsIt()
        {
            await new Spec("A session that left this channel is not found while the master still lists it")
                .GivenAsync(TheFriendIsOnlineAndKeptAsync)
                .And(TheFriendsSessionLeavesThisChannel)
                .WhenAsync(LookingForTheFriendAsync)
                .Then(NothingIsFound)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheNextLoginOfTheSameCharacterIsFound()
        {
            await new Spec("The next login of the same character is found")
                .GivenAsync(TheFriendIsOnlineAndKeptAsync)
                .And(TheFriendsSessionLeavesThisChannel)
                .And(TheFriendLogsInAgain)
                .WhenAsync(LookingForTheFriendAsync)
                .Then(TheNewSessionIsFound)
                .ExecuteAsync();
        }

        private async Task TheFriendIsOnlineAndKeptAsync()
        {
            Listed = new List<Subscriber> { Session(LeftSession) };
            await Cache.FindAsync(s => s.ConnectedCharacter?.Id == FriendId);
        }

        private void TheFriendsSessionLeavesThisChannel() => Cache.Forget(LeftSession);

        private void TheFriendLogsInAgain() => Listed = new List<Subscriber> { Session(LeftSession), Session(NewSession) };

        private async Task LookingForTheFriendAsync() => Found = await Cache.FindAsync(s => s.ConnectedCharacter?.Id == FriendId);

        private void NothingIsFound() => Assert.IsNull(Found);

        private void TheNewSessionIsFound() => Assert.AreEqual(NewSession, Found?.Id);

        private static Subscriber Session(long id) => new()
        {
            Id = id,
            Name = "friend",
            ConnectedCharacter = new Character { Id = FriendId, Name = "Friend" },
        };
    }
}
