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
    // Imports the game data again, from the folder the last import read, when the parser build or
    // the client files changed since. A database whose folder this server cannot see is only
    // checked against the parser build. Accounts are not seeded again, so a removed one stays removed.
    public static class GameDataRefresh
    {
        public static void EnsureCurrent(string connectionString, string[] args)
        {
            var parserBuild = GameDataFingerprint.OfParser(typeof(ParserBootstrap).Assembly);
            if (IsStale(connectionString, parserBuild) != null)
            {
                using var importLock = DatabaseReadiness.LockGameDataImport(connectionString);
                if (IsStale(connectionString, parserBuild) is { } stillStale)
                {
                    RunParser(stillStale, args);
                }
            }

            DatabaseReadiness.EnsureUpToDate(connectionString);
            if (DatabaseReadiness.DescribeGameData(DatabaseReadiness.ReadGameDataImport(connectionString), parserBuild) is { } stale)
            {
                throw new DatabaseNotReadyException(stale);
            }
        }

        // The folder to import again from, when the import is out of date and that folder can be read.
        private static string? IsStale(string connectionString, string parserBuild)
        {
            var import = DatabaseReadiness.ReadGameDataImport(connectionString);
            if (import == null || GameDataFingerprint.TryOfClientFiles(import.Folder) == null)
            {
                return null;
            }

            return !DatabaseReadiness.IsUpToDate(connectionString) || DatabaseReadiness.DescribeGameData(import, parserBuild) != null
                ? import.Folder
                : null;
        }

        private static void RunParser(string folder, string[] args)
        {
            var start = new ProcessStartInfo(DotnetHost()) { UseShellExecute = false };
            start.ArgumentList.Add(typeof(ParserBootstrap).Assembly.Location);
            start.ArgumentList.Add("--folder");
            start.ArgumentList.Add(folder);
            start.ArgumentList.Add(ParserCliOptions.SkipAccountsFlag);
            var config = Array.IndexOf(args, "--config");
            if (config > -1 && config + 1 < args.Length)
            {
                start.ArgumentList.Add("--config");
                start.ArgumentList.Add(args[config + 1]);
            }

            using var parser = Process.Start(start)
                ?? throw new DatabaseNotReadyException("The parser could not be started. Run it, then start the server again.");
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
