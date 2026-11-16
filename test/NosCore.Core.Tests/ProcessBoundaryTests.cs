//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpecLight;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NosCore.Core.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ProcessBoundaryTests
    {
        private readonly StringWriter _stderr = new();
        private TextWriter _previousError = Console.Error;

        [TestInitialize]
        public void Setup()
        {
            _previousError = Console.Error;
            Console.SetError(_stderr);
            Environment.ExitCode = 0;
        }

        [TestCleanup]
        public void Cleanup()
        {
            Console.SetError(_previousError);
            Environment.ExitCode = 0;
        }

        [TestMethod]
        public async Task AProcessThatFailsExitsWithAFailureCodeAndTheFullException()
        {
            await new Spec("A process that fails exits with code 1 and writes the whole exception to stderr")
                .WhenAsync(TheProcessFails)
                .Then(TheExitCodeShouldBe_, 1)
                .And(StderrShouldNameTheException)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExplainedFailureWritesOnlyItsMessage()
        {
            await new Spec("A failure that already tells the operator what to do is written as its message alone")
                .WhenAsync(TheProcessFailsWithAnExplainedFailure)
                .Then(TheExitCodeShouldBe_, 1)
                .And(StderrShouldBeOnlyTheMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AProcessThatEndsNormallyKeepsASuccessCode()
        {
            await new Spec("A process that ends normally keeps exit code 0")
                .WhenAsync(TheProcessEndsNormally)
                .Then(TheExitCodeShouldBe_, 0)
                .ExecuteAsync();
        }

        private Task TheProcessFails() =>
            ProcessBoundary.RunAsync(() => throw new InvalidOperationException("the database is not reachable"), false);

        private Task TheProcessFailsWithAnExplainedFailure() =>
            ProcessBoundary.RunAsync<TimeoutException>(() => throw new TimeoutException("Start the database, then the server."), false);

        private void StderrShouldBeOnlyTheMessage() =>
            Assert.AreEqual("Start the database, then the server.", _stderr.ToString().Trim());

        private Task TheProcessEndsNormally() => ProcessBoundary.RunAsync(() => Task.CompletedTask, false);

        private void TheExitCodeShouldBe_(int code) => Assert.AreEqual(code, Environment.ExitCode);

        private void StderrShouldNameTheException()
        {
            var written = _stderr.ToString();
            StringAssert.Contains(written, nameof(InvalidOperationException));
            StringAssert.Contains(written, "the database is not reachable");
            StringAssert.Contains(written, nameof(TheProcessFails));
        }
    }
}
