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

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class MessageSerializationTests
    {
        private static readonly Instant Start = Instant.FromUtc(2026, 11, 3, 11, 30);

        public record DatedMessage(Instant At) : IMessage
        {
            public System.Guid Id { get; set; }
        }

        private static T RoundTrip<T>(T value)
        {
            var options = new JsonSerializerOptions();
            WolverineHostExtensions.ConfigureMessageSerialization(options);
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, options), options)!;
        }

        [TestMethod]
        public void BazaarListingDatesSurviveAReply()
        {
            var reply = new Reply<List<BazaarLink>>(new List<BazaarLink>
            {
                new() { BazaarItem = new BazaarItemDto { DateStart = Start, Duration = 168 } }
            });

            Assert.AreEqual(Start, RoundTrip(reply).Value[0].BazaarItem!.DateStart);
        }

        [TestMethod]
        public void DatesSurviveAPolymorphicMessage()
        {
            var message = (DatedMessage)RoundTrip<IMessage>(new DatedMessage(Start));

            Assert.AreEqual(Start, message.At);
        }
    }
}
