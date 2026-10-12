//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;

public record GetAwaitingConnectionRequest(string? Name, string? Token, int SessionId) : IMasterRequest;

public record SetAwaitingConnectionRequest(long SessionId, string AccountName) : IMasterRequest;

public record StoreAuthCodeRequest(string AuthCode, string AccountName) : IMasterRequest;

public record RegisterSessionIpRequest(string AccountName, string IpAddress) : IMasterRequest;

public record UnregisterSessionIpRequest(string AccountName) : IMasterRequest;

public record GetSessionIpRequest(string AccountName) : IMasterRequest;
