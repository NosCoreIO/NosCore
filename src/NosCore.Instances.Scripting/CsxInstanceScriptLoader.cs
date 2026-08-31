//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NosCore.Instances.Abstractions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace NosCore.Instances.Scripting
{
    public class CsxInstanceScriptLoader(string folder)
    {
        private readonly ConcurrentDictionary<string, Task<IInstanceScript>> _scripts = new();

        // Roslyn costs ~1.3s to warm up and ~85ms a script after that, so compiling every
        // instance at boot would be about eleven seconds for no benefit. Compiled on first
        // entry instead, then kept.
        public Task<IInstanceScript> LoadAsync(string name) =>
            _scripts.GetOrAdd(name, CompileAsync);

        public string[] Available() => Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.csx")
            : [];

        private async Task<IInstanceScript> CompileAsync(string name)
        {
            var path = Path.Combine(folder, name + ".csx");
            var options = ScriptOptions.Default
                .WithReferences(typeof(IInstanceRun).Assembly)
                .WithImports("System", "NosCore.Instances.Abstractions");

            var script = CSharpScript.Create<IInstanceScript>(
                await File.ReadAllTextAsync(path).ConfigureAwait(false), options);

            // Diagnostics surface here, at load, rather than as a door onto an empty room.
            var diagnostics = script.Compile();
            if (!diagnostics.IsEmpty)
            {
                throw new InvalidOperationException(
                    $"{name}.csx did not compile:{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics)}");
            }

            var result = await script.RunAsync().ConfigureAwait(false);
            return result.ReturnValue
                ?? throw new InvalidOperationException($"{name}.csx returned no script instance.");
        }
    }
}
