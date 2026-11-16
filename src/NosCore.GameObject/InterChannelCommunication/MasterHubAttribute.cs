//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.InterChannelCommunication;

// On I<Name>Hub, generates a request record per method, <Name>HubClient for the channels and
// <Name>HubReceiver, which answers the requests on the master through <Name>Hub.
[AttributeUsage(AttributeTargets.Interface)]
public sealed class MasterHubAttribute : Attribute;
