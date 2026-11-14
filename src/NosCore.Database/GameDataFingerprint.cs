//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace NosCore.Database
{
    // An import is determined by the parser build and the client files it reads: when neither
    // fingerprint changed, importing again would write the same game data.
    public static class GameDataFingerprint
    {
        private const string OwnAssemblyPrefix = "NosCore.";

        // Covers every NosCore assembly the parser runs, so a change to an entity, an enum or a
        // sibling package counts as much as a change to the parser itself.
        public static string OfParser(Assembly parser)
        {
            var builder = new StringBuilder();
            foreach (var assembly in OwnAssemblies(parser).OrderBy(a => a.GetName().Name, StringComparer.Ordinal))
            {
                builder.Append(assembly.GetName().Name).Append(' ').Append(assembly.ManifestModule.ModuleVersionId).Append('\n');
            }

            return Hash(builder);
        }

        public static string OfClientFiles(string folder)
        {
            var root = new DirectoryInfo(folder);
            var builder = new StringBuilder();
            foreach (var (path, file) in root.EnumerateFiles("*", SearchOption.AllDirectories)
                         .Select(f => (Path.GetRelativePath(root.FullName, f.FullName).Replace('\\', '/'), f))
                         .OrderBy(f => f.Item1, StringComparer.Ordinal))
            {
                builder.Append(path).Append(' ').Append(file.Length).Append(' ').Append(file.LastWriteTimeUtc.Ticks).Append('\n');
            }

            return Hash(builder);
        }

        // Null when the folder, or anything below it, cannot be read from here.
        public static string? TryOfClientFiles(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return null;
            }

            try
            {
                return OfClientFiles(folder);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static string Hash(StringBuilder builder) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));

        private static IEnumerable<Assembly> OwnAssemblies(Assembly parser)
        {
            var seen = new Dictionary<string, Assembly>(StringComparer.Ordinal);
            var pending = new Stack<Assembly>([parser]);
            while (pending.TryPop(out var assembly))
            {
                if (assembly.GetName().Name is not { } name || !seen.TryAdd(name, assembly))
                {
                    continue;
                }

                foreach (var reference in assembly.GetReferencedAssemblies()
                             .Where(r => r.Name is { } referenced
                                         && referenced.StartsWith(OwnAssemblyPrefix, StringComparison.Ordinal)
                                         && !seen.ContainsKey(referenced)))
                {
                    pending.Push(Assembly.Load(reference));
                }
            }

            return seen.Values;
        }
    }
}
