//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.DevHost
{
    // Runs every server of a local stack as a child process and stops them together. The order does
    // not matter: the world server imports the game data and the others wait for it.
    public sealed partial class ServerProcesses(ILogger<ServerProcesses> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IHostApplicationLifetime lifetime) : IHostedService
    {
        private static readonly (string Prefix, string Assembly)[] Servers =
        [
            ("master", "NosCore.MasterServer"),
            ("login", "NosCore.LoginServer"),
            ("world", "NosCore.WorldServer"),
        ];

        private const int SigTerm = 15;

        private readonly List<Process> _running = [];
        private readonly Lock _console = new();
        private volatile bool _stopping;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var (prefix, assembly) in Servers)
            {
                var start = new ProcessStartInfo(DotnetHost())
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = AppContext.BaseDirectory,
                };
                start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, assembly + ".dll"));
                var process = new Process { StartInfo = start, EnableRaisingEvents = true };
                process.OutputDataReceived += (_, e) => Write(prefix, e.Data);
                process.ErrorDataReceived += (_, e) => Write(prefix, e.Data);
                process.Exited += (_, _) => OnExited(prefix, process);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                lock (_running)
                {
                    _running.Add(process);
                }
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopping = true;
            List<Process> running;
            lock (_running)
            {
                running = _running.Where(p => !p.HasExited).ToList();
            }

            if (running.Count == 0)
            {
                return;
            }

            logger.LogInformation(logLanguage[LogLanguageKey.STOPPING_SERVERS]);
            // A Ctrl+C in the terminal already reached them, as they share its console; a stop
            // request sent to this process alone did not.
            if (!OperatingSystem.IsWindows())
            {
                foreach (var process in running)
                {
                    _ = SendSignal(process.Id, SigTerm);
                }
            }

            try
            {
                await Task.WhenAll(running.Select(p => p.WaitForExitAsync(cancellationToken)));
            }
            catch (OperationCanceledException)
            {
                foreach (var process in running.Where(p => !p.HasExited))
                {
                    process.Kill(entireProcessTree: true);
                }
            }
        }

        private void OnExited(string prefix, Process process)
        {
            logger.Log(_stopping ? LogLevel.Information : LogLevel.Warning, logLanguage[LogLanguageKey.SERVER_PROCESS_EXITED], prefix,
                process.ExitCode);
            lock (_running)
            {
                if (_running.All(p => p.HasExited))
                {
                    lifetime.StopApplication();
                }
            }
        }

        private void Write(string prefix, string? line)
        {
            if (line == null)
            {
                return;
            }

            lock (_console)
            {
                Console.WriteLine($"[{prefix}] {line}");
            }
        }

        private static string DotnetHost()
        {
            var name = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
            if (Environment.ProcessPath is { } process && Path.GetFileName(process) == name)
            {
                return process;
            }

            // <root>/shared/Microsoft.NETCore.App/<version>/
            return Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", name));
        }

        [LibraryImport("libc", EntryPoint = "kill")]
        private static partial int SendSignal(int pid, int signal);
    }
}
