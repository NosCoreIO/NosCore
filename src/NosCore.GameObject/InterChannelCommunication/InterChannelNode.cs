//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;

namespace NosCore.GameObject.InterChannelCommunication;

public record InterChannelNode(string ServiceName, string QueueName)
{
    public const string MasterQueue = "noscore_master";

    public static InterChannelNode Master() => new("NosCore.MasterServer", MasterQueue);

    public static InterChannelNode For(string serviceName, string kind, string? host, int port) =>
        new(serviceName, Sanitize($"noscore_{kind}_{host}_{port}"));

    private static string Sanitize(string name) =>
        new(name.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray());
}
