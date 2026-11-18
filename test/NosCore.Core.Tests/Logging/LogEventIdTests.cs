//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using SpecLight;

namespace NosCore.Core.Tests.Logging
{
    [TestClass]
    public class LogEventIdTests
    {
        private readonly Mock<ILogger> _logger = new();
        private EventId _first;
        private EventId _second;

        [TestMethod]
        public void EveryLogKeyHasItsOwnId()
        {
            new Spec("No two log keys share an event id")
                .Then(EveryKeyGetsADistinctId)
                .Execute();
        }

        [TestMethod]
        public void AKeyAlwaysGetsTheSameId()
        {
            new Spec("The id depends on the key's name only, so it is the same in every language and build")
                .When(TakingTheIdOf_Twice, LogLanguageKey.HIT_APPLY_FAILED)
                .Then(BothIdsAreEqualAndNamed_, LogLanguageKey.HIT_APPLY_FAILED)
                .Execute();
        }

        [TestMethod]
        public void ALocalizedLogCallCarriesItsKeysId()
        {
            new Spec("Logging a localized message passes its key's event id, its text and its arguments")
                .When(LoggingTheLocalizedMessage_With_, LogLanguageKey.HIT_APPLY_FAILED, 42)
                .Then(TheLoggerReceivedTheIdOf_, LogLanguageKey.HIT_APPLY_FAILED)
                .Execute();
        }

        [TestMethod]
        public void AnExceptionGoesThroughWithTheId()
        {
            new Spec("The exception overload keeps the exception and adds the id")
                .When(LoggingTheLocalizedMessage_WithAnException, LogLanguageKey.HIT_APPLY_FAILED)
                .Then(TheLoggerReceivedTheIdOf_AndTheException, LogLanguageKey.HIT_APPLY_FAILED)
                .Execute();
        }

        private static void EveryKeyGetsADistinctId()
        {
            var ids = Enum.GetNames<LogLanguageKey>().Select(name => LogEventIds.For(name).Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
            Assert.IsTrue(ids.All(id => id >= 0));
        }

        private void TakingTheIdOf_Twice(LogLanguageKey key)
        {
            _first = LogEventIds.For(key.ToString());
            _second = LogEventIds.For(key.ToString());
        }

        private void BothIdsAreEqualAndNamed_(LogLanguageKey key)
        {
            Assert.AreEqual(_first, _second);
            Assert.AreEqual(key.ToString(), _first.Name);
        }

        private void LoggingTheLocalizedMessage_With_(LogLanguageKey key, int handle) =>
            _logger.Object.LogError(new LocalizedString(key.ToString(), "Failed to apply hit to entity {Handle}"), handle);

        private void LoggingTheLocalizedMessage_WithAnException(LogLanguageKey key) =>
            _logger.Object.LogError(new InvalidOperationException("boom"), new LocalizedString(key.ToString(), "text"));

        private void TheLoggerReceivedTheIdOf_(LogLanguageKey key) =>
            _logger.Verify(l => l.Log(LogLevel.Error, LogEventIds.For(key.ToString()), It.Is<It.IsAnyType>((v, _) => $"{v}" == "Failed to apply hit to entity 42"),
                null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        private void TheLoggerReceivedTheIdOf_AndTheException(LogLanguageKey key) =>
            _logger.Verify(l => l.Log(LogLevel.Error, LogEventIds.For(key.ToString()), It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(e => e.Message == "boom"), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
