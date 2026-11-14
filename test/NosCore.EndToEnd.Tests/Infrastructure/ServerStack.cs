//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // The master, login and world servers as the processes a deployment runs, built from this
    // checkout and pointed at a database of their own on ports of their own.
    public sealed class ServerStack : IAsyncDisposable
    {
        private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(90);

        private readonly List<ServerProcess> _servers = new();
        private readonly List<string> _configurations = new();

        private ServerStack(int loginPort)
        {
            LoginPort = loginPort;
        }

        public string Host => "127.0.0.1";

        public int LoginPort { get; }

        // One world server per channel, on one master and one login server.
        private const string WorldServer = "NosCore.WorldServer";
        private const string MasterServer = "NosCore.MasterServer";
        private const int RecentLines = 200;

        public static async Task<ServerStack> StartAsync(string databaseName, int channels = 2)
        {
            var root = RepositoryRoot();
            var binaries = Path.Combine(root, "build", "net11.0");
            var stack = new ServerStack(FreePort());
            var environment = new Dictionary<string, string>
            {
                ["MASTER_PORT"] = FreePort().ToString(),
                ["LOGIN_PORT"] = stack.LoginPort.ToString(),
            };

            try
            {
                var shared = stack.WriteConfiguration(root, databaseName, 0);
                await stack.StartAsync(binaries, MasterServer, "Successfully Loaded", shared, environment);
                await stack.StartAsync(binaries, "NosCore.LoginServer", "Listening Port", shared, environment);
                for (var channel = 0; channel < channels; channel++)
                {
                    var port = FreePort();
                    var world = new Dictionary<string, string>(environment) { ["WORLD_PORT"] = port.ToString() };
                    await stack.StartAsync(binaries, WorldServer, "Successfully Loaded",
                        stack.WriteConfiguration(root, databaseName, port), world);
                }
            }
            catch
            {
                await stack.DisposeAsync();
                throw;
            }

            return stack;
        }

        // One server on its own, for a scenario about how a server starts: what it wrote by the
        // time the expected line arrived. It is stopped before this returns.
        public static async Task<IReadOnlyList<string>> StartAloneAsync(string name, string databaseName, string expectedLine)
        {
            var root = RepositoryRoot();
            await using var stack = new ServerStack(FreePort());
            var environment = new Dictionary<string, string>
            {
                ["MASTER_PORT"] = FreePort().ToString(),
                ["LOGIN_PORT"] = stack.LoginPort.ToString(),
                ["WORLD_PORT"] = FreePort().ToString(),
            };
            await stack.StartAsync(Path.Combine(root, "build", "net11.0"), name, expectedLine,
                stack.WriteConfiguration(root, databaseName, 0), environment);
            return stack._servers.SelectMany(s => s.Output).ToList();
        }

        // A connection the server closed is rarely explained by an error line, so that case also
        // gets the last lines of every level and the servers that are no longer running.
        public string Diagnose(bool connectionClosed)
        {
            var report = new StringBuilder();
            Append(report, "World server errors", Errors(WorldServer).TakeLast(6));
            Append(report, "Master server errors", Errors(MasterServer).TakeLast(6));
            if (connectionClosed)
            {
                Append(report, "Servers that stopped", _servers.Where(s => s.HasExited).Select(s => $"{s.Name} (exit code {s.ExitCode})"));
                Append(report, "World server log", _servers.Where(s => s.Name == WorldServer).SelectMany(s => s.Recent.TakeLast(25)));
            }

            return report.ToString();
        }

        private static void Append(StringBuilder report, string title, IEnumerable<string> lines)
        {
            var kept = lines.Select(line => "  " + (line.Length > 300 ? line[..300] : line)).ToList();
            if (kept.Count > 0)
            {
                report.AppendLine().Append(title).Append(':').AppendLine().AppendJoin(Environment.NewLine, kept);
            }
        }

        // An error line and the lines of its exception.
        public IEnumerable<string> Errors(string server)
        {
            foreach (var process in _servers.Where(s => s.Name == server))
            {
                var inError = false;
                foreach (var line in process.Output)
                {
                    var levelled = line.Length > 4 && line[4] == ' ' && line[..4].All(char.IsUpper);
                    inError = levelled ? line.StartsWith("EROR", StringComparison.Ordinal) : inError;
                    if (inError && !line.StartsWith("   at Microsoft.", StringComparison.Ordinal) && !line.StartsWith("   at System.", StringComparison.Ordinal))
                    {
                        yield return line.Length > 300 ? line[..300] : line;
                    }
                }
            }
        }

        private async Task StartAsync(string binaries, string name, string readyLine, string configuration,
            Dictionary<string, string> environment)
        {
            var assembly = Path.Combine(binaries, name + ".dll");
            if (!File.Exists(assembly))
            {
                throw new FileNotFoundException($"{name} is not built. Run 'dotnet build NosCore.sln' first.", assembly);
            }

            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = binaries,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add(assembly);
            start.ArgumentList.Add("--config");
            start.ArgumentList.Add(configuration);
            foreach (var (key, value) in environment)
            {
                start.Environment[key] = value;
            }

            var server = new ServerProcess(name, Process.Start(start) ?? throw new InvalidOperationException($"{name} did not start"));
            _servers.Add(server);
            await server.WaitForAsync(readyLine, StartTimeout);
        }

        // The address a world server announces is in its file, so each channel has its own copy.
        private string WriteConfiguration(string root, string databaseName, int worldPort)
        {
            var target = Path.Combine(Path.GetTempPath(), "noscore-e2e-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(target);
            _configurations.Add(target);
            foreach (var file in Directory.GetFiles(Path.Combine(root, "configuration"), "*.yml"))
            {
                var text = File.ReadAllText(file);
                text = Regex.Replace(text, @"(?m)^(\s*)Database: noscore\s*$", "$1Database: " + databaseName);
                text = Regex.Replace(text, @"(?m)^DisplayPort: \d+\s*$", "DisplayPort: " + worldPort);
                File.WriteAllText(Path.Combine(target, Path.GetFileName(file)), text);
            }

            return target;
        }

        private static string RepositoryRoot()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "NosCore.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new DirectoryNotFoundException("NosCore.sln was not found above " + AppContext.BaseDirectory);
        }

        private static int FreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        public async ValueTask DisposeAsync()
        {
            for (var i = _servers.Count - 1; i >= 0; i--)
            {
                await _servers[i].StopAsync();
            }

            foreach (var configuration in _configurations)
            {
                try
                {
                    Directory.Delete(configuration, true);
                }
                catch (IOException)
                {
                }
            }
        }

        private sealed class ServerProcess
        {
            private readonly Process _process;

            public ServerProcess(string name, Process process)
            {
                Name = name;
                _process = process;
                // A test host that is stopped half way must not leave servers behind.
                AppDomain.CurrentDomain.ProcessExit += (_, _) => KillQuietly();
                _process.OutputDataReceived += (_, e) => Keep(e.Data);
                _process.ErrorDataReceived += (_, e) => Keep(e.Data);
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }

            public string Name { get; }

            public bool HasExited => _process.HasExited;

            public int ExitCode => _process.ExitCode;

            public ConcurrentQueue<string> Output { get; } = new();

            // The last lines at every level, for the report on a closed connection.
            public ConcurrentQueue<string> Recent { get; } = new();

            public async Task WaitForAsync(string text, TimeSpan timeout)
            {
                var waited = Stopwatch.StartNew();
                while (waited.Elapsed < timeout)
                {
                    if (Output.Any(line => line.Contains(text, StringComparison.Ordinal)))
                    {
                        return;
                    }

                    if (_process.HasExited)
                    {
                        break;
                    }

                    await Task.Delay(200);
                }

                throw new InvalidOperationException(
                    $"{Name} did not report '{text}' within {timeout.TotalSeconds:0} s." +
                    $"{Environment.NewLine}{string.Join(Environment.NewLine, Output.TakeLast(30))}");
            }

            public async Task StopAsync()
            {
                if (_process.HasExited)
                {
                    return;
                }

                _process.Kill(true);
                await _process.WaitForExitAsync();
                _process.Dispose();
            }

            private void KillQuietly()
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(true);
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }

            // The message bus reports every message it moves at debug level, several a second.
            // That is left out everywhere; other debug lines only go to the recent tail.
            private static readonly string[] BusChatter =
            [
                "message(s) from", "Successfully processed message", "Enqueued for sending", " from local://", " from postgresql://",
                "Started processing", "Finished processing", "reply listener", "Sending requested reply"
            ];

            private void Keep(string? line)
            {
                if (line == null)
                {
                    return;
                }

                var debug = line.StartsWith("DBUG", StringComparison.Ordinal);
                if (!debug)
                {
                    Output.Enqueue(line);
                }

                if (debug && BusChatter.Any(phrase => line.Contains(phrase, StringComparison.Ordinal)))
                {
                    return;
                }

                Recent.Enqueue(line);
                while (Recent.Count > RecentLines)
                {
                    Recent.TryDequeue(out _);
                }
            }
        }
    }
}
