//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Messaging;
using System.Collections.Generic;
using System.Text.Json;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class MessageSerializationTests
    {
        private static readonly Instant Start = Instant.FromUtc(2026, 11, 3, 11, 30);

        public record UnlistedMessage(Instant At) : IMessage
        {
            public System.Guid Id { get; set; }
        }

        public class ConstructionProbe
        {
            public static int Constructed;

            public ConstructionProbe()
            {
                Constructed++;
            }
        }

        private static T RoundTrip<T>(T value)
        {
            var options = new JsonSerializerOptions();
            WolverineHostExtensions.ConfigureMessageSerialization(options);
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, options), options)
                ?? throw new AssertFailedException("The message deserialized to null.");
        }

        [TestMethod]
        public void BazaarListingDatesSurviveAReply()
        {
            var reply = new Reply<List<BazaarLink>>(new List<BazaarLink>
            {
                new() { BazaarItem = new BazaarItemDto { DateStart = Start, Duration = 168 }, ItemInstance = new ItemInstanceDto() }
            });

            Assert.AreEqual(Start, RoundTrip(reply).Value[0].BazaarItem.DateStart);
        }

        private static IMessage? Read(string typeName)
        {
            var options = new JsonSerializerOptions();
            WolverineHostExtensions.ConfigureMessageSerialization(options);
            return JsonSerializer.Deserialize<IMessage>($"{{\"_type\":\"{typeName}\",\"_object\":{{}}}}", options);
        }

        [TestMethod]
        public void DatesSurviveAPolymorphicMessage()
        {
            var message = (MailData)RoundTrip<IMessage>(new MailData { ItemInstance = new ItemInstanceDto { ItemDeleteTime = Start } });

            Assert.AreEqual(Start, message.ItemInstance.NotNull().ItemDeleteTime);
        }

        [TestMethod]
        public void MessagesFromOlderNodesNamingTheAssemblyAreRead()
        {
            Assert.IsInstanceOfType<DeleteMailData>(Read(typeof(DeleteMailData).AssemblyQualifiedName.NotNull()));
        }

        [TestMethod]
        public void OnlyMessagesDeclaredByNosCoreAreRead()
        {
            Assert.ThrowsExactly<JsonException>(() => Read(typeof(UnlistedMessage).AssemblyQualifiedName.NotNull()));
        }

        [TestMethod]
        public void TypesThatAreNotMessagesAreNeverConstructed()
        {
            ConstructionProbe.Constructed = 0;

            Assert.ThrowsExactly<JsonException>(() => Read(typeof(ConstructionProbe).AssemblyQualifiedName.NotNull()));
            Assert.AreEqual(0, ConstructionProbe.Constructed);
        }
    }
}
