//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core;
using NosCore.Data.WebApi;
using System.Collections.Concurrent;

namespace NosCore.GameObject.InterChannelCommunication;

public class MasterClientList
{
    public readonly ConcurrentDictionary<string, ChannelInfo> Channels = new();
    public readonly ConcurrentDictionary<string, ConcurrentDictionary<long, Subscriber>> ConnectedAccounts = new();

    public int ConnectionCounter { get; set; }
    public ConcurrentDictionary<string, long> ReadyForAuth { get; } = new();

    public ConcurrentDictionary<string, string> AuthCodes { get; } = new();
}
