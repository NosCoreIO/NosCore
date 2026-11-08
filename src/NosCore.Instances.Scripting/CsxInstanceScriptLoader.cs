//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Instances.Scripting
{
    public class CsxInstanceScriptLoader(string folder) : IInstanceScriptLoader
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<IInstanceScript>>> _scripts = new();

        // Roslyn costs ~1.3s to warm up and ~85ms a script after that, so compiling every
        // instance at boot would be about eleven seconds for no benefit. Compiled on first
        // use instead, then kept.
        public Task<IInstanceScript> LoadAsync(string name) =>
            _scripts.GetOrAdd(name, key => new Lazy<Task<IInstanceScript>>(() => CompileAsync(key))).Value;

        private async Task<IInstanceScript> CompileAsync(string name)
        {
            // A script runs with the server's full rights, so only files directly in the
            // instances folder are loaded, whatever the database row names.
            if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name
                || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains(".."))
            {
                throw new ArgumentException($"{name} is not a script name.", nameof(name));
            }

            var path = Path.Combine(folder, name + ".csx");
            var options = ScriptOptions.Default
                .WithReferences(typeof(IInstanceRun).Assembly, typeof(Game18NConstString).Assembly)
                .WithImports("System", "NosCore.Instances.Abstractions", "NosCore.Packets.Enumerations");

            var script = CSharpScript.Create<IInstanceScript>(await File.ReadAllTextAsync(path), options);

            // Diagnostics surface here, at load, rather than as a door onto an empty room.
            var diagnostics = script.Compile();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{name}.csx did not compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }

            var result = await script.RunAsync();
            return result.ReturnValue
                ?? throw new InvalidOperationException($"{name}.csx returned no script instance.");
        }
    }
}
