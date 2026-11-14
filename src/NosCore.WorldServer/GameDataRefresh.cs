//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database;
using NosCore.Parser;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace NosCore.WorldServer
{
    // Imports the game data when the database has none, and again, from the folder the last import
    // read, when the parser build or the client files changed since. A database whose folder this
    // server cannot see is only checked against the parser build. Accounts are only seeded by the
    // first import, so a removed one stays removed.
    public static class GameDataRefresh
    {
        public static void EnsureCurrent(string connectionString, string[] args)
        {
            var parserBuild = GameDataFingerprint.OfParser(typeof(ParserBootstrap).Assembly);
            if (ImportToRun(connectionString, parserBuild) != null)
            {
                using var importLock = DatabaseReadiness.LockGameDataImport(connectionString);
                if (ImportToRun(connectionString, parserBuild) is { } import)
                {
                    RunParser(import, args);
                }
            }

            DatabaseReadiness.EnsureUpToDate(connectionString);
            if (DatabaseReadiness.DescribeGameData(DatabaseReadiness.ReadGameDataImport(connectionString), parserBuild) is { } stale)
            {
                throw new DatabaseNotReadyException(stale);
            }
        }

        // Null when nothing has to run. A database never imported gets a first import, with the
        // client files downloaded and the default accounts created; an out-of-date one is imported
        // again from its folder when that folder can be read.
        private static ParserRun? ImportToRun(string connectionString, string parserBuild)
        {
            var import = DatabaseReadiness.ReadGameDataImport(connectionString);
            if (import == null)
            {
                return DatabaseReadiness.CanConnect(connectionString) ? new ParserRun(null, SeedAccounts: true) : null;
            }

            if (GameDataFingerprint.TryOfClientFiles(import.Folder) == null)
            {
                return null;
            }

            return !DatabaseReadiness.IsUpToDate(connectionString) || DatabaseReadiness.DescribeGameData(import, parserBuild) != null
                ? new ParserRun(import.Folder, SeedAccounts: false)
                : null;
        }

        private sealed record ParserRun(string? Folder, bool SeedAccounts);

        private static void RunParser(ParserRun run, string[] args)
        {
            var start = new ProcessStartInfo(DotnetHost()) { UseShellExecute = false };
            // A published world server carries the parser without its own dependency manifest, and
            // its own one lists every assembly the parser needs.
            start.ArgumentList.Add("exec");
            start.ArgumentList.Add("--depsfile");
            start.ArgumentList.Add(Path.ChangeExtension(typeof(GameDataRefresh).Assembly.Location, ".deps.json"));
            start.ArgumentList.Add(typeof(ParserBootstrap).Assembly.Location);
            if (run.Folder != null)
            {
                start.ArgumentList.Add("--folder");
                start.ArgumentList.Add(run.Folder);
            }

            if (!run.SeedAccounts)
            {
                start.ArgumentList.Add(ParserCliOptions.SkipAccountsFlag);
            }

            var config = Array.IndexOf(args, "--config");
            if (config > -1 && config + 1 < args.Length)
            {
                start.ArgumentList.Add("--config");
                start.ArgumentList.Add(args[config + 1]);
            }

            using var parser = Process.Start(start)
                ?? throw new DatabaseNotReadyException("The parser could not be started. Run it.");
            parser.WaitForExit();
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
    }
}
