//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;

namespace NosCore.Core
{
    // Logging may not be configured yet when a host fails to build, so failures go to stderr.
    public static class ProcessBoundary
    {
        public static Task RunAsync(Func<Task> run, bool holdConsoleOnFailure) =>
            RunAsync<OperatorMessageNeverThrown>(run, holdConsoleOnFailure);

        // TExplained already tells the operator what to do, so only its message is written.
        public static async Task RunAsync<TExplained>(Func<Task> run, bool holdConsoleOnFailure)
            where TExplained : Exception
        {
            try
            {
                await run();
                return;
            }
            catch (TExplained explained)
            {
                await Console.Error.WriteLineAsync(explained.Message);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString());
            }

            Environment.ExitCode = 1;
            if (holdConsoleOnFailure && !Console.IsInputRedirected)
            {
                Console.ReadLine();
            }
        }

        private sealed class OperatorMessageNeverThrown : Exception;
    }
}
