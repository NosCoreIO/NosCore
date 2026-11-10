//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Parser;

public sealed record ParserCliOptions(string? Folder)
{
    public string? ShipRoutes { get; init; }
    public bool HasShipRoutes => !string.IsNullOrWhiteSpace(ShipRoutes);

    public bool HasFolder => !string.IsNullOrWhiteSpace(Folder);

    /// <summary>Select mutually exclusive folder or ship-route import inputs and reject missing ship-route paths.</summary>
    public static ParserCliOptions Parse(string[] args)
    {
        string? folder = null;
        string? shipRoutes = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--ship-routes")
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("-", System.StringComparison.Ordinal))
                {
                    throw new System.ArgumentException("--ship-routes requires a file path.");
                }
                shipRoutes = args[++i];
            }
            else if (arg.StartsWith("--ship-routes=", System.StringComparison.Ordinal))
            {
                shipRoutes = arg["--ship-routes=".Length..];
                if (string.IsNullOrWhiteSpace(shipRoutes))
                {
                    throw new System.ArgumentException("--ship-routes requires a file path.");
                }
            }
            else if ((arg == "--folder" || arg == "-f") && i + 1 < args.Length)
            {
                folder = args[++i];
            }
            else if (arg.StartsWith("--folder=", System.StringComparison.Ordinal))
            {
                folder = arg["--folder=".Length..];
            }
        }
        if (folder != null && shipRoutes != null)
        {
            throw new System.ArgumentException("Use either --folder or --ship-routes.");
        }
        return new ParserCliOptions(folder) { ShipRoutes = shipRoutes };
    }
}
