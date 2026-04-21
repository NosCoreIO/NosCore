//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Parser;

public sealed record ParserCliOptions(string? Folder)
{
    public bool HasFolder => !string.IsNullOrWhiteSpace(Folder);

    public static ParserCliOptions Parse(string[] args)
    {
        string? folder = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if ((arg == "--folder" || arg == "-f") && i + 1 < args.Length)
            {
                folder = args[++i];
            }
            else if (arg.StartsWith("--folder=", System.StringComparison.Ordinal))
            {
                folder = arg["--folder=".Length..];
            }
        }
        return new ParserCliOptions(folder);
    }
}
