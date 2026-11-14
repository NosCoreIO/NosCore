//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Parser;

public sealed record ParserCliOptions(string? Folder, bool SkipAccounts = false)
{
    public const string SkipAccountsFlag = "--skip-accounts";

    [MemberNotNullWhen(true, nameof(Folder))]
    public bool HasFolder => !string.IsNullOrWhiteSpace(Folder);

    public static ParserCliOptions Parse(string[] args)
    {
        string? folder = null;
        var skipAccounts = false;
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
            else if (arg == SkipAccountsFlag)
            {
                skipAccounts = true;
            }
        }
        return new ParserCliOptions(folder, skipAccounts);
    }
}
